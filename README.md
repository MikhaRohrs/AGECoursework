# AGECoursework

## Info
- Press G to generate a city
- Press M to enable/disable using Manhattan distance (you will need to re-generate to see results)
- Press C to show mouse cursor
- Press ESC to exit
- Colour codes for buildings: Black = road, White = residential, Green = park, Red = industrial, Yellow = market, Grey = business
- If using editor make sure the road thickness element in the city generator game object is set to 0

## Rubric
- Satisfactory (40-49%): *Generate a city with course districts that are filled with grey boxes*
-- This can be seen just by pressing 'G', although the boxes have been coloured to denote different districts and the roads between them.
- Good (50-59%) *City generated with buildings matching the district they're in, as well as using the Manhattan distance*
-- This can again be seen by looking at the colours of the buildings. Some districts, such as parks, will won't have any buildings, so they're just green, while a market district won't be 100% market buildings (right now it's 75% market, 20% residential and 5% business). As for manhattan distance, M can be pressed to enable and disable it.
- Very Good (60-69%) *City generated with thematic buildings and using Manhattan distance, as well as adding support for subgrids to devide each grid into blocks and modifying how buildings are assigned to districts.* -- This can be seen by the mix of coloured buildings for some districts. You can also see that each grid is devided into blocks although these roads are the same size as the major roads deviding different districts.
