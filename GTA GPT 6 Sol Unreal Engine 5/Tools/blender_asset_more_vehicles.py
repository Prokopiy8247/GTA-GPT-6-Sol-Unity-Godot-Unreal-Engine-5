def add_wheels(coll, front, rear, half_width, radius):
    for x in (front,rear):
        for side in (-1,1):
            y=side*half_width
            cyl(coll,"Rubber tire",(x,y,radius+.045),radius,.25,"rubber",14,(math.pi/2,0,0))
            cyl(coll,"Metal rim",(x,y+side*.135,radius+.045),radius*.61,.032,"silver",12,(math.pi/2,0,0))
            cyl(coll,"Wheel hub",(x,y+side*.159,radius+.045),radius*.20,.035,"graphite",10,(math.pi/2,0,0))

def glasshouse(coll, x0, x1, yhalf, z0, ztop, body_color):
    verts=[(x0,-yhalf,z0),(x1,-yhalf,z0),(x1,yhalf,z0),(x0,yhalf,z0),
           (x0+.37,-yhalf*.88,ztop),(x1-.39,-yhalf*.88,ztop),(x1-.39,yhalf*.88,ztop),(x0+.37,yhalf*.88,ztop)]
    mesh(coll,"Cabin shell",verts,[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],body_color)
    mesh(coll,"Front windshield",[(x1,-yhalf*.92,z0+.04),(x1,yhalf*.92,z0+.04),(x1-.39,yhalf*.85,ztop-.03),(x1-.39,-yhalf*.85,ztop-.03)],[(0,1,2,3)],"glass")
    mesh(coll,"Rear windshield",[(x0,-yhalf*.92,z0+.04),(x0+.37,-yhalf*.85,ztop-.03),(x0+.37,yhalf*.85,ztop-.03),(x0,yhalf*.92,z0+.04)],[(0,1,2,3)],"glass")
    for side in (-1,1):
        y=side*yhalf*1.012
        mesh(coll,"Side glass",[(x0+.08,y,z0+.07),(x0+.39,side*yhalf*.90,ztop-.04),(x1-.42,side*yhalf*.90,ztop-.04),(x1-.08,y,z0+.07)],[(0,1,2,3)],"glass")

def lamp_details(coll, nose, tail, width):
    for side in (-1,1):
        box(coll,"Headlamp",(nose+.045,side*width*.38,.95),(.04,.30,.15),"cream",.017)
        box(coll,"Taillamp",(tail-.045,side*width*.38,.95),(.04,.31,.15),"coral",.017)
    box(coll,"Nose grille",(nose+.055,0,.69),(.04,width*.46,.25),"graphite",.015)
    box(coll,"Front bumper",(nose+.12,0,.48),(.16,width*1.01,.12),"graphite",.015)
    box(coll,"Rear bumper",(tail-.12,0,.48),(.16,width*1.01,.12),"graphite",.015)

coll=start_asset("Sedan")
box(coll,"Long sedan body",(0,0,.81),(4.83,1.89,.75),"sand",.15)
box(coll,"Hood",(1.53,0,1.19),(1.52,1.78,.15),"sand",.045)
box(coll,"Trunk lid",(-1.75,0,1.18),(1.12,1.78,.13),"sand",.045)
glasshouse(coll,-1.25,.87,.78,1.20,1.83,"sand")
add_wheels(coll,1.49,-1.53,.94,.40)
lamp_details(coll,2.42,-2.42,1.89)
for side in (-1,1):
    box(coll,"Door handles",(-.10,side*.96,1.04),(.85,.035,.043),"graphite",.01)
    box(coll,"Body chrome",(0,side*.969,.74),(3.8,.025,.035),"silver",.009)
join_asset(coll,"Sedan")

coll=start_asset("PoliceCar")
box(coll,"Cruiser body",(0,0,.78),(4.95,1.94,.72),"police",.15)
box(coll,"Door white field",(-.18,-.981,.89),(1.66,.028,.43),"white",.01)
box(coll,"Door white field",(-.18,.981,.89),(1.66,.028,.43),"white",.01)
box(coll,"Hood",(1.68,0,1.14),(1.50,1.85,.15),"white",.045)
box(coll,"Trunk",(-1.74,0,1.14),(1.10,1.84,.13),"white",.045)
glasshouse(coll,-1.25,.92,.80,1.15,1.79,"police")
add_wheels(coll,1.59,-1.58,.98,.41)
lamp_details(coll,2.48,-2.48,1.94)
box(coll,"Push bumper",(2.65,0,.74),(.20,1.63,.49),"graphite",.04)
box(coll,"Lightbar base",(-.13,0,1.85),(.83,.73,.10),"graphite",.018)
box(coll,"Lightbar red",(-.13,-.24,1.94),(.58,.28,.13),"red",.018)
box(coll,"Lightbar blue",(-.13,.24,1.94),(.58,.28,.13),"blue",.018)
for side in (-1,1):
    box(coll,"Cruiser stripe",(-.35,side*.999,.65),(2.38,.03,.10),"turquoise",.01)
    box(coll,"Badge",(.13,side*1.011,.96),(.22,.015,.20),"amber",.01)
join_asset(coll,"PoliceCar")

coll=start_asset("SUV")
box(coll,"SUV raised shell",(0,0,1.02),(4.82,2.08,1.12),"turquoise_dark",.14)
box(coll,"Lower skid plate",(0,0,.52),(4.66,1.94,.19),"graphite",.03)
glasshouse(coll,-1.95,1.28,.86,1.54,2.21,"turquoise_dark")
add_wheels(coll,1.48,-1.53,1.04,.46)
lamp_details(coll,2.43,-2.43,2.08)
for side in (-1,1):
    box(coll,"Running board",(0,side*1.065,.45),(3.38,.17,.11),"graphite",.022)
    line(coll,"Roof rack",(-1.57,side*.77,2.25),(1.02,side*.77,2.25),.035,"graphite",8)
    box(coll,"Door inset",(-.15,side*1.054,1.16),(1.56,.025,.23),"turquoise",.01)
box(coll,"Spare tire",(-2.46,0,1.09),(.19,.83,.83),"rubber",.10)
join_asset(coll,"SUV")

coll=start_asset("Pickup")
box(coll,"Truck chassis",(0,0,.81),(5.23,1.98,.72),"sand",.12)
box(coll,"Cab lower",(.87,0,1.18),(2.10,1.86,.24),"sand",.05)
glasshouse(coll,-.15,1.93,.80,1.29,2.02,"sand")
box(coll,"Bed deck",(-1.52,0,1.24),(2.13,1.76,.13),"graphite",.03)
for side in (-1,1):
    box(coll,"Bed rail",(-1.52,side*.94,1.51),(2.15,.14,.45),"sand",.03)
box(coll,"Tailgate",(-2.57,0,1.51),(.13,1.84,.45),"sand",.03)
add_wheels(coll,1.67,-1.65,1.0,.43)
lamp_details(coll,2.63,-2.64,1.98)
box(coll,"Rear bed latch",(-2.65,0,1.54),(.025,.27,.08),"graphite",.01)
join_asset(coll,"Pickup")

coll=start_asset("Van")
box(coll,"Cargo van body",(-.03,0,1.31),(5.12,2.0,1.68),"cream",.16)
box(coll,"Lower graphite sill",(-.03,0,.57),(5.0,2.01,.21),"graphite",.025)
box(coll,"Front windscreen",(2.545,0,1.77),(.028,1.77,1.00),"glass",.02)
for side in (-1,1):
    box(coll,"Front door window",(1.40,side*1.015,1.73),(1.30,.028,.83),"glass",.02)
    box(coll,"Sliding door seam",(-.58,side*1.024,1.25),(.018,.015,1.27),"graphite")
    box(coll,"Sliding door handle",(-.27,side*1.039,1.30),(.28,.018,.07),"graphite",.008)
    box(coll,"Company diagonal",(-1.55,side*1.029,1.41),(.87,.018,.20),"turquoise",.01)
add_wheels(coll,1.57,-1.63,1.02,.41)
lamp_details(coll,2.54,-2.59,2.00)
box(coll,"Van roof rack",(-.27,0,2.23),(3.84,1.75,.10),"graphite",.02)
join_asset(coll,"Van")

coll=start_asset("SportsCar")
box(coll,"Low wide sports shell",(0,0,.61),(4.60,2.04,.57),"coral",.17)
mesh(coll,"Wedge bonnet",[(.45,-.90,.93),(2.32,-.90,.80),(2.32,.90,.80),(.45,.90,.93)],[(0,1,2,3)],"coral")
glasshouse(coll,-1.23,.50,.80,.94,1.43,"graphite")
add_wheels(coll,1.42,-1.43,1.04,.38)
lamp_details(coll,2.31,-2.31,2.04)
box(coll,"Side air intake",(-.75,-1.034,.65),(.53,.02,.23),"graphite",.018)
box(coll,"Side air intake",(-.75,1.034,.65),(.53,.02,.23),"graphite",.018)
box(coll,"Rear wing",(-1.94,0,1.40),(.37,2.11,.075),"graphite",.024)
for side in (-1,1):
    box(coll,"Wing support",(-1.94,side*.65,1.18),(.09,.06,.41),"graphite",.012)
box(coll,"Racing stripe",(.89,0,.94),(1.54,.20,.015),"cream")
join_asset(coll,"SportsCar")

coll=start_asset("Motorcycle")
for x in (-.83,.85):
    cyl(coll,"Bike tire",(x,0,.47),.45,.18,"rubber",16,(math.pi/2,0,0))
    cyl(coll,"Bike spoke hub",(x,-.106,.47),.26,.025,"silver",12,(math.pi/2,0,0))
line(coll,"Main frame",(-.75,0,.48),(.24,0,1.05),.07,"graphite",10)
line(coll,"Down frame",(.24,0,1.05),(.83,0,.48),.06,"steel",10)
box(coll,"Engine block",(-.10,0,.62),(.53,.39,.47),"steel",.055)
ico(coll,"Fuel tank",(.27,0,1.20),(.52,.34,.26),"turquoise",1)
box(coll,"Seat",(-.48,0,1.07),(.89,.45,.15),"graphite",.055)
line(coll,"Front fork",(.86,-.18,.49),(.53,-.18,1.50),.04,"silver",9)
line(coll,"Front fork",(.86,.18,.49),(.53,.18,1.50),.04,"silver",9)
line(coll,"Handlebar",(.56,-.52,1.54),(.56,.52,1.54),.035,"graphite",9)
box(coll,"Headlight",(.64,0,1.38),(.16,.25,.22),"cream",.04)
box(coll,"Rear light",(-.98,0,1.07),(.10,.21,.10),"coral",.02)
box(coll,"Exhaust",(-.71,.27,.69),(1.05,.10,.10),"silver",.03)
join_asset(coll,"Motorcycle")
save_master()
