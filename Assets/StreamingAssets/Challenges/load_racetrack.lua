function PrintSomething()
    Print("something", {r=1})
end
function Loop()
end

LoadMap("racetrack");
SetPosition("player1", {-10, 8, 0});
for i=1,10 do
    Spawn("sphere", {position={i, 10+i*10, i}, mass=10, size={5,5,5}, bounciness=0.99});
end
-- Spawn("sphere", {position= {x= 0, y= 10, z= 10}, mass=-1, size={x=10,y=10,z=10}, });
Spawn("gate", {position={10, 0, 10}, size={20}, color={150, 150, 250, 120}, options={triggerObjects={"player1"}, func=PrintSomething }});
