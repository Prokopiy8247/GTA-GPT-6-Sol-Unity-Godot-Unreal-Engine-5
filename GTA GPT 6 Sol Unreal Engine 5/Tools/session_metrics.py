"""Read Codex JSONL usage for this benchmark without counting cumulative snapshots twice.

Usage: python Tools/session_metrics.py [root-session-id]
"""

from __future__ import annotations

import json
import sys
from datetime import datetime
from pathlib import Path


ROOT_ID = "01a0ceda-a35f-7c52-bce5-f9391fa450d6"
LOG_DIR = Path.home() / ".codex" / "sessions" / "2026" / "09" / "23"
FIELDS = (
    "input_tokens",
    "cached_input_tokens",
    "cache_write_input_tokens",
    "output_tokens",
    "reasoning_output_tokens",
    "total_tokens",
)
STANDARD_USD_PER_MILLION = {
    "uncached_input": 2.00,
    "cached_input": 0.20,
    "cache_write_input": 2.50,
    "output": 10.00,
}


def response_cost(usage: dict) -> float:
    input_tokens = usage.get("input_tokens", 0) or 0
    cached = usage.get("cached_input_tokens", 0) or 0
    cache_write = usage.get("cache_write_input_tokens", 0) or 0
    output = usage.get("output_tokens", 0) or 0
    uncached = input_tokens - cached - cache_write
    if uncached < 0:
        raise ValueError("Input categories overlap unexpectedly")
    long_context = input_tokens > 272_000
    input_multiplier = 2.0 if long_context else 1.0
    output_multiplier = 1.5 if long_context else 1.0
    rates = STANDARD_USD_PER_MILLION
    return (
        uncached * rates["uncached_input"] * input_multiplier
        + cached * rates["cached_input"] * input_multiplier
        + cache_write * rates["cache_write_input"] * input_multiplier
        + output * rates["output"] * output_multiplier
    ) / 1_000_000


def read_session(path: Path) -> dict | None:
    meta = None
    records = []
    model = None
    effort = None
    for line in path.open("r", encoding="utf-8"):
        try:
            event = json.loads(line)
        except json.JSONDecodeError:
            continue  # The live session can end with an incomplete line.
        if event.get("type") == "session_meta" and meta is None:
            meta = event.get("payload", {})
        elif event.get("type") == "turn_context":
            context = event.get("payload", {})
            model = context.get("model", model)
            effort = context.get("effort", effort)
        elif event.get("type") == "token_usage_record":
            payload = event.get("payload", {})
            if isinstance(payload.get("thread_token_usage"), dict):
                records.append(payload)
    if not meta:
        return None
    own_records = [r for r in records if r.get("thread_id") == meta.get("id")]
    latest = own_records[-1].get("thread_token_usage") if own_records else None
    usage_sum = {field: sum((r.get("usage") or {}).get(field, 0) or 0 for r in own_records) for field in FIELDS}
    long_context = [r for r in own_records if (r.get("usage") or {}).get("input_tokens", 0) > 272_000]
    return {
        "path": str(path),
        "thread_id": meta.get("id"),
        "session_id": meta.get("session_id"),
        "parent_thread_id": meta.get("parent_thread_id"),
        "model": model,
        "effort": effort,
        "cwd": meta.get("cwd"),
        "records": len(own_records),
        "usage": latest,
        "response_usage_sum_matches_cumulative": latest == usage_sum if latest else None,
        "max_response_input_tokens": max(((r.get("usage") or {}).get("input_tokens", 0) for r in own_records), default=0),
        "long_context_responses": len(long_context),
        "token_only_api_equivalent_cost_usd": round(sum(response_cost(r.get("usage") or {}) for r in own_records), 6),
    }


def main() -> None:
    root_id = sys.argv[1] if len(sys.argv) > 1 else ROOT_ID
    sessions = [s for p in LOG_DIR.glob("*.jsonl") if (s := read_session(p))]
    by_id = {s["thread_id"]: s for s in sessions}

    def belongs_to_root(session: dict) -> bool:
        seen = set()
        current = session
        while current and current["thread_id"] not in seen:
            sid = current["thread_id"]
            if sid == root_id:
                return True
            seen.add(sid)
            current = by_id.get(current["parent_thread_id"])
        return False

    relevant = sorted((s for s in sessions if belongs_to_root(s)), key=lambda x: x["path"])
    totals = {field: 0 for field in FIELDS}
    for session in relevant:
        usage = session["usage"]
        if usage:
            for field in FIELDS:
                totals[field] += usage.get(field, 0) or 0
    output = {
        "root_session_id": root_id,
        "captured_at_local": datetime.now().astimezone().isoformat(),
        "sessions": relevant,
        "totals": totals,
        "pricing_usd_per_million": STANDARD_USD_PER_MILLION,
        "token_only_api_equivalent_cost_usd": round(sum(s["token_only_api_equivalent_cost_usd"] for s in relevant), 6),
        "long_context_responses": sum(s["long_context_responses"] for s in relevant),
        "total_tokens_equals_input_plus_output": totals["total_tokens"] == totals["input_tokens"] + totals["output_tokens"],
        "missing_usage_threads": [s["thread_id"] for s in relevant if not s["usage"]],
    }
    print(json.dumps(output, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
