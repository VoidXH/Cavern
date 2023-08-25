# Guidelines for object-based movie audio
## Glossary
* **Object**: A moving audio source.
* **Aggregate object**: A single object playing the sound of multiple objects
  for storage and/or performance optimization.
* **Bed**: A non-moving audio source matched with one or more output channels.
  The bed is generally a 7.1.2 mix of music, ambience, and environmental reverb.

## Scope
In development context, movie audio objects and Cavern `Source`s are
equivalent. This document does not apply for game development where the
`Listener`'s `MaximumSources` and `Range` applies and an infinite number of
objects can and shall be used in any configuration and arrangement.

## Renderer setup guidelines
Because object-based audio renderers made for movie use do not have distance
simulations (that effect is baked into beds, objects, or aggregate objects) or
stereo panning (stereo objects are separated to multiple objects), the following
assertions have to be made to the rendering environment:
* `Listener.Range` shall be larger than the maximum radius in the environment.
* `Listener.MaximumSources` shall be 16 for home cinema use cases and 128 for
  commercial cinema use cases.
* `Source.SpatialBlend` shall be 1 for all `Source`s.
* `Source.VolumeRolloff` shall be `Disabled` for all `Source`s.
* `Source.Position`'s absolute value shall not exceed
  `Listener.EnvironmentSize`. This is undefined behavior for some renderers.

These settings should provide a very close or equal listening experience to
official decoders of the respective codecs.

## Home cinema audio guidelines
### System limitations
Home cinema systems are heavily constrained by bandwidth limitations, and the
following specifications shall be followed to adapt for the weakest link:
* Do not mix channels and objects, either allocate all tracks as a bed or none
  of them. Although most codecs support it, this behavior was never discovered
  in movie or music tracks, and as such, it's considered undefined behavior.
  Movies are almost exclusively object-only and music are generally
  channel-only.
* Do not scale objects, `Size` shall be 0. While most codecs support an object
  size field, no other value was ever found in any track, and as such, it's
  considered undefined behavior. Furthermore, the exact renderer implementation
  is a black box and very likely differs from Cavern.
* The number of `Source`s should be less or equal than 16 and an even number. No
  odd values were ever observed and are considered undefined behavior. While
  some codecs, like Enhanced AC-3 with JOC support more than 16 tracks, these
  are limited to extra subframes that are out of range for said codec's maximum
  commercial bitrate.

### Safe approach
Because object sizing and mixed typing is disencouraged, in this approach, the
entire mix is pre-rendered to a fixed channel layout, which is then exported in
the metadata as either completely channel-based, joint channel-based, or object-
based content. An easy preparation for this is writing the rendered output (the
result of `Listener.Render`) as a first pass into a channel-based file format,
such as RIFF WAVE. By using the `EnhancedAC3Merger` project under
`CavernSamples`, that WAVE file can be converted to a completely channel-based
Enhanced AC-3, without JOC.

Real-world examples are overwhelmingly joint channel-based, with the inactive
channels' objects being moved back to the default location (in EAC-3's case it's
the front left channel) without any encoded audio, but the active flag is not
disabled. In these cases, 7.1.2 is mostly active because the before mentioned
bed mixing techniques, and other channels up to 9.1.6 occasionally get active.
This behavior can lead to a quickly drawn conclusion that the content is a 7.1.2
downmix - which is not. Cavern's encoder does not move the unused channels back
to the default position, and some newer movie encoders also behave this way.

Having a completely channel-based mix does not degrade audio quality in any way.
Since home setups currently are limited to 9.1.6, having all channels available
will render correctly on any possible channel arrangement. For smaller rooms,
even matrix downmixing is fine.

### Graphic approach
To appeal to the object visualizers and display some movement, but still respect
the limit of 16 objects, aggregation of object groups is one solution. Objects
close to each other can be mixed together at an averaged position until the
object limit is met. This setup will always show object movement when the
original mix contained any, but because of the averaging and re-rendering, this
method is considered lossy and should not be used, unless the priority is
demoing that the system in fact renders objects correctly. From all methods,
this is the only one that results in degraded audio quality.

### Combined approach
The recommended and minorly future-proof method is based on the safe approach.
When there are lots of object movement, a fallback to completely channel-based
rendering is the only viable way of preventing the entire soundstage and having
an accurate mix, thus, it's the only method that should be used. However, when
the number of active bed channels and dynamic objects is less than the codec's
object limit, it's possible to convey all of these, and store both channels and
objects as output tracks that will all be objects in the resulting file. Because
of the occasional additional channel usage on larger layouts than 9.1.6, and no
loss of spacial precision, this is the recommended method of storing spatial
mixes. Looking good in the visualizer should never be a priority for any mix,
because the audible result won't be different from a static mix on today's
systems, but the visualizer is still used incorrectly by some to draw
conclusions of a mix's quality. The combined approach serves as a middle ground
for all use cases, sometimes displaying active and precise object movement,
while always being spatially accurate. This is the current general practice.

### Known differences
#### Dolby Atmos Renderer
* While Cavern considers a value of `|1|` as the wall of the room, in the case
  of the Dolby Atmos Reference Player, all values over `|0.5|` are considered
  equal and at the wall of the room. This exaggerates in-room object movement.
  The same behavior can be reproduced with Cavern by halving the values of
  `Listener.EnvironmentSize`.
* The object-embedded channels in a Dolby Atmos mix do not match neither their
  official speaker placement guides or the Cavern Driver's angles. Because of
  this, to be accurate, Cavernize, and only Cavernize, is using those angles if
  a preset layout is applied instead of Cavern Driver. These positions are found
  in ChannelPrototype.Conts.cs as `ChannelPrototype.AlternativePositions`.

## Commercial cinema audio guidelines
Unless the maximum `Source` count of 128 is surpassed, Cavern's
`EnvironmentWriter`s handle complete transfer of a rendered mix to commercial
formats. The target standard's `EnvironmentWriter` should be used, and keep in
mind that while Dolby Atmos is based on ADM BWF, they are not bidirectionally
compatible, and the respective descendant of `EnvironmentWriter` should be used.
