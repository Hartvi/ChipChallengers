-- Gates sit after route merges so the easy road and technical shortcuts are both valid.
Scenario({
    id = "4a9e4b50-6fd4-42ab-9d13-2499fd31a752",
    title = "Quarry Climb",
    recordVersion = 1,
    scenarioLogicVersion = 1,
    completionType = "timeTrial",
    rules = {
        forbiddenChipTypes = {},
        maxChips = -1,
        physics = { fixedDeltaTime = 0.001, solverIterations = 1, solverVelocityIterations = 3 }
    }
})

LoadMap("quarry_climb")
SetPosition("player1", {0, 4.350, -20})
SetRotation("player1", {0, 0, 0})

Race({
    radius = 8,
    color = {80, 210, 130, 180},
    start = {0, 3.350, 0},
    checkpoints = {
        {10, 27, -75},
        {125, 39, -10},
    },
    finish = {90, 39, 60}
})
