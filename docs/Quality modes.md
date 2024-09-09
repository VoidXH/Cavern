# Cavern quality modes
| Quality  | Doppler/pitch | Resampling   | Object downmix | Angle precision | Asymmetric mixing |
|----------|---------------|--------------|----------------|-----------------|-------------------|
| Perfect  | High          | Catmull-Rom  | All channels   | Arcsine         | As-is             |
| High     | High          | Linear       | All channels   | Arcsine         | Triangulated      |
| Medium   | Low           | Nearest      | First channel  | Linear          | Triangulated      |
| Low      | Disabled      | Nearest      | First channel  | Linear          | Triangulated      |
