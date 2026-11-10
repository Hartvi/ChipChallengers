Challenge({
    id = "b4220d19-b894-4275-9614-84921cbfebde",
    title = "Circuit 1",
    recordVersion = 1,
    challengeLogicVersion = 1,
    completionType = "timeTrial",
    rules = { forbiddenChipTypes = {}, maxChips = -1 }
})

LoadMap("circuit1");

SetPosition("player1", {0, 23, 0});

Race({
    radius = 2.5,
    start = {-1,22,10},
    checkpoints = {
        {-3,22,50},
        {-17,25,364},
        {189,81,501},
        {333,50,209},
        {276,41,-506},
        {230,58,-302},
        {140,82,-510},
        {137,100,-237},
        {16,120,-570},
        {516,53,-576},
        {23,22,-260},
        {-36,26,-140},
    },
    finish = {-1,22,10}
})


