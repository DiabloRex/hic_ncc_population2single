# hic_ncc_population2single
Covert populational hic ncc format matrix to single cell matrix

## Usage: 
hic_ncc_population2single [1.Input file] [2.output file] [3.threshold] [4.method]
method: 0 -> PointMax, 1 -> EdgeMax, 2 -> TotalRandom

## Example:
hic_ncc_population2single O48.ncc O48.single.point.ncc 2 0
