coll = start_asset("Car")
box(coll, "Main sculpted body", (0, 0, 0.75), (4.42, 1.82, 0.62), "turquoise", 0.16)
box(coll, "Hood shoulder", (1.37, 0, 1.10), (1.58, 1.73, 0.18), "turquoise", 0.07)
box(coll, "Rear deck", (-1.56, 0, 1.12), (1.08, 1.72, 0.18), "turquoise", 0.05)
box(coll, "Underside", (0, 0, 0.43), (4.10, 1.63, 0.18), "graphite", 0.07)

# One-piece angular glasshouse, deliberately lower at the rear.
verts = [(-1.27,-0.78,1.13),(0.82,-0.78,1.13),(0.82,0.78,1.13),(-1.27,0.78,1.13),
         (-0.77,-0.68,1.72),(0.27,-0.68,1.72),(0.27,0.68,1.72),(-0.77,0.68,1.72)]
faces = [(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7),(0,3,2,1)]
mesh(coll, "Cabin shell", verts, faces, "graphite")
mesh(coll, "Windshield", [(0.78,-0.64,1.16),(0.78,0.64,1.16),(0.24,0.62,1.68),(0.24,-0.62,1.68)], [(0,1,2,3)], "glass")
mesh(coll, "Rear glass", [(-1.23,-0.64,1.16),(-0.74,-0.62,1.68),(-0.74,0.62,1.68),(-1.23,0.64,1.16)], [(0,1,2,3)], "glass")
for side in (-1,1):
    y = side*0.716
    mesh(coll, "Side front glass", [(0.15,y,1.17),(0.15,side*0.62,1.65),(-0.24,side*0.62,1.65),(-0.24,y,1.17)],[(0,1,2,3)],"glass")
    mesh(coll, "Side rear glass", [(-0.32,y,1.17),(-0.32,side*0.62,1.65),(-0.68,side*0.62,1.65),(-1.13,y,1.17)],[(0,1,2,3,4)],"glass")
    box(coll, "Door accent", (-0.04,side*0.926,0.87), (1.52,0.023,0.045), "coral", 0.01)
    box(coll, "Door handle", (0.05,side*0.947,1.07), (0.22,0.045,0.045), "graphite", 0.01)
    box(coll, "Mirror stem", (0.62,side*0.89,1.25),(0.12,0.19,0.06),"graphite")
    box(coll, "Mirror", (0.63,side*1.01,1.29),(0.22,0.12,0.12),"turquoise",0.03)
    for x in (-1.48,1.43):
        cyl(coll,"Tire",(x,side*0.91,0.41),0.39,0.24,"rubber",16,(math.pi/2,0,0))
        cyl(coll,"Rim",(x,side*1.045,0.41),0.235,0.035,"silver",12,(math.pi/2,0,0))
        cyl(coll,"Hub",(x,side*1.065,0.41),0.075,0.045,"graphite",10,(math.pi/2,0,0))
        for angle in range(0,360,72):
            a=math.radians(angle)
            box(coll,"Rim spoke",(x+0.13*math.cos(a),side*1.07,0.41+0.13*math.sin(a)),(0.11,0.025,0.045),"graphite",0.01)

box(coll,"Front grill",(2.224,0,0.76),(0.035,0.79,0.28),"graphite",0.015)
for side in (-1,1):
    box(coll,"Headlight",(2.222,side*0.69,0.91),(0.045,0.29,0.16),"cream",0.025)
    box(coll,"Taillight",(-2.228,side*0.65,0.93),(0.045,0.34,0.15),"coral",0.025)
box(coll,"Front bumper",(2.28,0,0.50),(0.15,1.88,0.13),"graphite",0.03)
box(coll,"Rear bumper",(-2.28,0,0.50),(0.15,1.88,0.13),"graphite",0.03)
box(coll,"Rear spoiler",(-1.90,0,1.22),(0.35,1.65,0.055),"graphite",0.02)
box(coll,"License front",(2.32,0,0.66),(0.014,0.35,0.10),"white")
box(coll,"License rear",(-2.32,0,0.67),(0.014,0.35,0.10),"white")
join_asset(coll,"Car")
save_master()
