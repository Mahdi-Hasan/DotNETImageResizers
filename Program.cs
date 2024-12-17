using DotNETImageResizer;

const int expectedSize = 512;
string inputFolderPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\standard_test_images"));
string outputReportPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\Report"));
string outputFilesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\CompressedImages\"));

var results =await new ImageCompressor().RunBenchmarksAsync(inputFolderPath, outputFilesPath, expectedSize);

new ReportGenerator().GenerateReport(results, outputReportPath);
