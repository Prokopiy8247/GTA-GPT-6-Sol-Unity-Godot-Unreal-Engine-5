# Harborline — Unity

Harborline — небольшой оригинальный 3D sandbox с открытым миром, созданный в Unity 6. Игра сразу запускается в свободном режиме: можно исследовать город, управлять транспортом, использовать оружие и взаимодействовать с полицией и городскими точками.

Проект не связан с Rockstar Games или Take-Two Interactive. GTA используется только как общеизвестное описание жанра в названии репозитория и исходном prompt.

## Самый простой запуск

Готовые сборки находятся на странице [GitHub Releases](https://github.com/Prokopiy8247/GTA-GPT-6-Sol-Unity-Godot-Unreal-Engine-5/releases/latest). Unity и Blender для запуска игры не нужны.

### Windows 10/11, 64-bit

1. Скачайте `Harborline-Windows-x64.zip`.
2. Нажмите на ZIP правой кнопкой и выберите **Извлечь всё**.
3. Откройте распакованную папку и запустите `Harborline.exe`.
4. Если Windows SmartScreen покажет предупреждение для неподписанного приложения, выберите **Подробнее** → **Выполнить в любом случае**.

Не переносите `Harborline.exe` отдельно: папка `Harborline_Data` и остальные файлы должны лежать рядом с ним.

### macOS

1. Скачайте `Harborline-macOS-Universal.zip`.
2. Дважды нажмите ZIP, затем перетащите `Harborline.app` в папку **Applications**.
3. При первом запуске нажмите на `Harborline.app` правой кнопкой и выберите **Open / Открыть**.
4. Если macOS всё ещё блокирует неподписанную сборку, откройте **System Settings → Privacy & Security** и нажмите **Open Anyway** рядом с сообщением о Harborline.

Сборка не подписана и не нотарифицирована Apple, поэтому первое предупреждение системы ожидаемо.

## Управление

| Клавиша | Действие |
| --- | --- |
| `WASD` | Движение пешком или управление транспортом |
| Мышь | Камера; правая кнопка — прицел; левая — атака |
| `Shift` | Бег |
| `Space` | Прыжок / ручник / набор высоты |
| `Ctrl` или `C` | Присесть / снизиться |
| `F` | Сесть в транспорт или выйти |
| `E` | Взаимодействовать |
| `R` | Перезарядка |
| `1`–`8` или колесо мыши | Выбор оружия |
| `V` | Вид от первого лица |
| `M` | Карта |
| `F1` | Sandbox-меню |
| `F2` | Полная справка по управлению |
| `Esc` | Пауза / закрыть меню |

## Запуск исходного проекта

1. Установите [Unity Hub](https://unity.com/download).
2. Через Unity Hub установите Editor **6000.6.0f1** с нужным модулем **Windows Build Support** или **Mac Build Support**.
3. Клонируйте весь репозиторий или скачайте **Code → Download ZIP**.
4. В Unity Hub нажмите **Add → Add project from disk** и выберите папку `GTA GPT 6 Sol Unity`.
5. Дождитесь импорта файлов. Откройте `Assets/GTA/Scenes/Harborline.unity` и нажмите кнопку Play.

Первый импорт может занять несколько минут. Папки `Library`, `Temp` и `Logs` Unity создаст автоматически.

## Самостоятельная сборка

В меню Unity доступны команды:

- **GTA → Build Windows Player**;
- **GTA → Build macOS Player**;
- **GTA → Build Windows + macOS Players**.

Результат появится в папке `Builds`. Подробности об устройстве проекта и ограничениях находятся в [DEVELOPMENT.md](DEVELOPMENT.md) и [FEATURE_MATRIX.md](FEATURE_MATRIX.md).

## Исходный prompt

Полный prompt, по которому создавался проект, сохранён в [GPT-6-Sol_Unity_GTA_BlenderMCP_Prompt.md](GPT-6-Sol_Unity_GTA_BlenderMCP_Prompt.md).
