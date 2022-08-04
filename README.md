# OMG
## (Opus Magnum Gif-CMD)

## Arguments
* Any path pointing to a solution file will be interpreted as the desired solution
* `start=<cycle>` Starts the gif at the specified cycle
* `end=<cycle>` Ends the gif at the specified cycle
* `fpc=<frames>` Sets the frames per cycle of the gif
* `speed=<speed>` Honestly? I don't know. High number makes the gif faster usually
* `out=<path>` Outputs the gif in the specified path
* `framing={default, eq, bounds}` Sets the framing mode. Default is what the game normally does, eq uses only Glyphs of Equilibrium to determine the framing, and bounds allows you to set your own bounds (measured in pixels!)
* `min=<x>,<y>` Sets the top left point of the framing on `bounds` mode. Must be used with `max`
* `max=<x>,<y>` Sets the bottom right point of the framing on `bounds` mode. Must be used with `min`
