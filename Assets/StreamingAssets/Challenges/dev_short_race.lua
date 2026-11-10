Challenge({
    id = "ac988843-9a35-4d6c-afbd-f6169a90ca87",
    title = "Race Track",
    recordVersion = 1,
    challengeLogicVersion = 1,
    completionType = "timeTrial",
    rules = { forbiddenChipTypes = {}, maxChips = -1 }
})

LoadMap("racetrack");

SetPosition("player1", {0, 10, 0});

Race({
    radius = 2.5,
    start = {10,7,10},
    checkpoints = {
        {15,7,20},
    },
    finish = {20,7,40}
})

Spawn("booster", {
    position = { 5, 6, 0 },
    size     = { 3, 3, 3 },
    color    = { 255, 0, 0, 128 },  -- semi-transparent red
    options  = {
        force  = { 0, 5000, 0 },      -- upward boost
    }
})


