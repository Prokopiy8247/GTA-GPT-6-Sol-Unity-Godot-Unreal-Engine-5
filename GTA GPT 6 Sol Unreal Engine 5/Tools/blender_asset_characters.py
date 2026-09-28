def humanoid(name, jacket, pants, trim, skin, police=False, pedestrian=False):
    coll = start_asset(name)
    for side in (-1, 1):
        y = side * 0.15
        line(coll, "Thigh", (0, y, 1.00), (0.015, y, 0.56), 0.105, pants, 10)
        line(coll, "Shin", (0.015, y, 0.56), (0.045, y, 0.15), 0.087, pants, 10)
        box(coll, "Sneaker", (0.12, y, 0.075), (0.33, 0.20, 0.14), "graphite", 0.045)
        box(coll, "Shoe strip", (0.16, y, 0.043), (0.26, 0.205, 0.025), trim, 0.008)
    box(coll, "Hip", (0, 0, 1.015), (0.43, 0.46, 0.23), pants, 0.07)
    box(coll, "Torso", (0, 0, 1.33), (0.43, 0.57, 0.56), jacket, 0.12)
    box(coll, "Waist belt", (0, 0, 1.08), (0.44, 0.50, 0.075), "graphite", 0.025)
    box(coll, "Belt clasp", (0.231, 0, 1.08), (0.025, 0.115, 0.075), "silver", 0.008)
    for side in (-1, 1):
        shoulder = (0, side*0.33, 1.50)
        elbow = (0.055, side*0.39, 1.18)
        wrist = (0.13, side*0.36, 0.98)
        line(coll, "Upper arm", shoulder, elbow, 0.09, jacket, 10)
        line(coll, "Forearm", elbow, wrist, 0.078, jacket, 10)
        ico(coll, "Hand", (0.145,side*0.36,0.925), (0.07,0.075,0.10), skin, 1)
        box(coll, "Cuff", (0.125,side*0.36,1.015),(0.12,0.15,0.04),trim,0.01)
    cyl(coll,"Neck",(0,0,1.63),0.085,0.16,skin,10)
    ico(coll,"Head",(0.015,0,1.775),(0.175,0.152,0.216),skin,2)
    ico(coll,"Nose",(0.174,0,1.765),(0.055,0.053,0.070),skin,1)
    for side in (-1,1):
        ico(coll,"Ear",(0.015,side*0.16,1.76),(0.046,0.032,0.065),skin,1)
        ico(coll,"Eye",(0.159,side*0.069,1.807),(0.012,0.023,0.014),"dark",1)
    if police:
        box(coll,"Uniform collar",(0.17,0,1.56),(0.06,0.34,0.05),"white",0.01)
        box(coll,"Chest badge",(0.23,-0.14,1.44),(0.03,0.10,0.11),"amber",0.01)
        box(coll,"Chest radio",(0.23,0.14,1.45),(0.05,0.09,0.12),"graphite",0.01)
        box(coll,"Police cap brim",(0.13,0,1.99),(0.37,0.38,0.035),"graphite",0.012)
        box(coll,"Police cap",(0,0,2.025),(0.30,0.33,0.12),"police",0.035)
        box(coll,"Cap badge",(0.18,0,2.035),(0.015,0.075,0.065),"silver")
        box(coll,"Holster",(-0.04,0.285,1.00),(0.17,0.085,0.23),"graphite",0.025)
    elif pedestrian:
        ico(coll,"Hair",(-0.023,0,1.93),(0.178,0.157,0.105),"hair",1)
        box(coll,"Shirt stripe",(0.22,0,1.37),(0.025,0.47,0.065),trim,0.01)
        box(coll,"Backpack",(-0.30,0,1.32),(0.18,0.34,0.38),"coral",0.06)
        for side in (-1,1):
            box(coll,"Backpack strap",(0.15,side*0.22,1.33),(0.035,0.055,0.45),"graphite",0.01)
    else:
        ico(coll,"Hair crown",(-0.025,0,1.933),(0.18,0.156,0.108),"hair",1)
        box(coll,"Hood rim",(-0.105,0,1.70),(0.17,0.32,0.23),"graphite",0.045)
        box(coll,"Jacket zipper",(0.231,0,1.35),(0.012,0.023,0.43),trim)
        box(coll,"Jacket pocket L",(0.221,-0.14,1.21),(0.018,0.12,0.07),"graphite",0.008)
        box(coll,"Jacket pocket R",(0.221,0.14,1.21),(0.018,0.12,0.07),"graphite",0.008)
        for side in (-1,1):
            box(coll,"Sunglass lens",(0.175,side*0.075,1.813),(0.023,0.12,0.055),"glass",0.012)
    join_asset(coll,name)

humanoid("Player","graphite","steel","turquoise","skin_light")
humanoid("Pedestrian","sand","blue","cream","skin",pedestrian=True)
humanoid("Police","police","graphite","turquoise","skin_light",police=True)
save_master()
