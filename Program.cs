using DotNETImageResizer;


const string inputFolderPath = @"C:\Users\dev_v\OneDrive\Desktop\Learning\DotNETImageResizer";
const string outputReportPath = @"C:\Users\dev_v\OneDrive\Desktop\Learning\DotNETImageResizer";

var compressor = new ImageCompressor();
var results = await compressor.RunBenchmarksAsync(inputFolderPath);
compressor.GenerateReport(results, outputReportPath);