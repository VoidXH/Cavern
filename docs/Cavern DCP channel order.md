# Cavern DCP channel order
This is the recommended DCP channel ordering, and also the recommended wiring
for Digital Cinema Packages, the standard content delivery format for commercial
theatres. This documentation is for DCP producing entities, not for home movies.

Cavern DCPs only take advantage of currently or always unused channels, thus
systems built for Cavern are perfectly backwards compatible, including 7.1,
hearing/visually impaired tracks, and all sync signals. Cavern is using a 7.1.2
bed only, with the Top Surround channels as Left/Right ceiling arrays. Cavern XL
adds an additional Bottom Surround channel array. Speakers that are part of the
Bottom Surround array shall be placed into the stairs in the theatre room.

| Channel | 5.1 | 7.1 SDDS | 7.1 DS | Auro 11.1 | Cavern | Cavern XL | Notes                                         |
|--------:|:---:|:--------:|:------:|:---------:|:------:|:---------:|-----------------------------------------------|
| 1       | L   | L        | L      | L         | L      | L         | Front Left                                    |
| 2       | R   | R        | R      | R         | R      | R         | Front Right                                   |
| 3       | C   | C        | C      | C         | C      | C         | Front Center                                  |
| 4       | LFE | LFE      | LFE    | LFE       | LFE    | LFE       | Screen Low Frequency Effects                  |
| 5       | Ls  | Ls       | Lss    | Ls        | Lss    | Lss       | Left (Side) Surround                          |
| 6       | Rs  | Rs       | Rss    | Rs        | Rss    | Rss       | Right (Side) Surround                         |
| 7       | HI  | HI       | HI     | Ltf       | HI     | HI        | Hearing Impaired Dialog / Left Top Front      |
| 8       | VI  | VI       | VI     | Rtf       | VI     | VI        | Visually Impaired Narrative / Right Top Front |
| 9       | -   | LC       | -      | Ctf       | Lts    | Lts       | Left Center / Top Surround / Center Top Front |
| 10      | -   | RC       | -      | Gv        | Rts    | Rts       | Right Center / Top Surround / God's Voice     |
| 11      | -   | -        | Lrs    | Lts       | Lrs    | Lrs       | Left Rear / Top Surround                      |
| 12      | -   | -        | Rrs    | Rts       | Rrs    | Rrs       | Right Rear / Top Surround                     |
| 13      | MD  | MD       | MD     | MD        | MD     | MD        | Motion Data Sync                              |
| 14      | ES  | ES       | ES     | ES        | ES     | ES        | External Sync Signal                          |
| 15      | SL  | SL       | SL     | SL        | SL     | SL        | Sign Language Video                           |
| 16      | -   | -        | -      | -         | -      | Bs        | Bottom Surround                               |
