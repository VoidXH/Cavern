# Cavern DCP channel order
This is the recommended DCP channel ordering, and also the recommended wiring.
Cavern DCPs only take advantage of currently or always unused channels, thus
systems built for Cavern are perfectly backwards compatible, including 7.1,
hearing/visually impaired tracks, and all sync signals. Cavern is using a 7.1.2
bed only, with the Top Surround channels as Left/Right ceiling arrays. Cavern XL
adds an additional Bottom Surround channel.

| Channel | 5.1 | 7.1 SDDS | 7.1 DS | Cavern | Cavern XL | Notes                        |
|---------|:---:|:--------:|:------:|:------:|:---------:|------------------------------|
| 1       | L   | L        | L      | L      | L         | Front Left                   |
| 2       | R   | R        | R      | R      | R         | Front Right                  |
| 3       | C   | C        | C      | C      | C         | Front Center                 |
| 4       | LFE | LFE      | LFE    | LFE    | LFE       | Screen Low Frequency Effects |
| 5       | Ls  | Ls       | Lss    | Lss    | Lss       | Left (Side) Surround         |
| 6       | Rs  | Rs       | Rss    | Rss    | Rss       | Right (Side) Surround        |
| 7       | HI  | HI       | HI     | HI     | HI        | Hearing Impaired Dialog      |
| 8       | VI  | VI       | VI     | VI     | VI        | Visually Impaired Narrative  |
| 9       | -   | LC       | -      | Lts    | Lts       | Left Center / Top Surround   |
| 10      | -   | RC       | -      | Rts    | Rts       | Right Center / Top Surround  |
| 11      | -   | -        | Lrs    | Lrs    | Lrs       | Left Rear Surround           |
| 12      | -   | -        | Rrs    | Rrs    | Rrs       | Right Rear Surround          |
| 13      | MD  | MD       | MD     | MD     | MD        | Motion Data Sync             |
| 14      | ES  | ES       | ES     | ES     | ES        | External Sync Signal         |
| 15      | SL  | SL       | SL     | SL     | SL        | Sign Language Video          |
| 16      | -   | -        | -      | -      | Bs        | Bottom Surround              |

# Channel angles
For home and studios with a single speaker per channel, the recommended angles
(+/- 10°) are:

| Channel             | W / Y | H / X |
|---------------------|:-----:|:-----:|
| Front Left          | -30°  | 0°    |
| Front Right         | 30°   | 0°    |
| Front Center        | 0°    | 0°    |
| Left Side Surround  | -110° | 0°    |
| Right Side Surround | 110°  | 0°    |
| Left Rear Surround  | -150° | 0°    |
| Right Rear Surround | 150°  | 0°    |
| Front Left Center   | -15°  | 0°    |
| Front Right Center  | 15°   | 0°    |
| Left Top Surround   | -70°  | -45°  |
| Right Top Surround  | 70°   | -45°  |
| Bottom Surround     | 0°    | 90°   |
