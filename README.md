# Image Compression Libraries Comparison

## Overview

This project compares the performance and efficiency of three popular .NET image-processing libraries:
- ImageSharp
- Magick.NET
- SkiaSharp

The goal is to analyze and benchmark image compression capabilities across different image sizes and formats.

## Project Objectives

The primary objectives of this comparison are to measure and compare:
- Compression time
- Memory usage
- Final compressed file size

## Image Sources

Sample images have been sourced from the following repositories:
- [Yavuz Çeliker's Sample Images](https://github.com/yavuzceliker/sample-images/)
- [Imazen Resizer Example Images](https://github.com/imazen/resizer/tree/develop/examples/images)

## Detailed Findings

A comprehensive report of the compression benchmarks is available in the [Google Sheets document](https://docs.google.com/spreadsheets/d/1wxENh8kDOpWUOJT2Pp-S5Jlfuiw5I_TFfOPBURXOktw/edit?usp=sharing).

## Libraries Tested

### 1. ImageSharp
- .NET image processing library
- Cross-platform support
- High-performance image manipulation

### 2. Magick.NET
- ImageMagick wrapper for .NET
- Extensive image processing capabilities
- Support for numerous image formats

### 3. SkiaSharp
- .NET binding for Google's Skia graphics library
- High-performance 2D graphics
- Used in many cross-platform applications

## Methodology

1. Load images from the specified repositories
2. Compress images using each library
3. Measure and record:
   - Compression time
   - Memory consumption
   - Resulting file size
4. Analyze and compare results

## Requirements

- .NET SDK
- NuGet Package Manager
- Visual Studio or compatible IDE

## Installation

Clone the repository:
```bash
git clone https://github.com/Mahdi-Hasan/DotNETImageResizers
```

Install required NuGet packages:
```bash
dotnet restore
```

## Running the Benchmark

```bash
dotnet run
```

## Results Interpretation

Refer to the [Google Sheets report](https://docs.google.com/spreadsheets/d/1wxENh8kDOpWUOJT2Pp-S5Jlfuiw5I_TFfOPBURXOktw/edit?usp=sharing) for a detailed breakdown of:
- Compression performance
- Memory efficiency
- File size reduction

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## Acknowledgments

- [Yavuz Çeliker](https://github.com/yavuzceliker)
- [Imazen Resizer Project](https://github.com/imazen/resizer)
