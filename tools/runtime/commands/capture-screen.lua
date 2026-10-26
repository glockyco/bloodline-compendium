-- Asks the engine for a screenshot of the viewport, with the interface unless args.ui is false, as the Shot
-- console command takes it. The engine writes the file at the end of the frame.
console(args.ui == false and "Shot" or "Shot showui")
return { requested = true }
