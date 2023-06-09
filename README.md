# Tiktoken

[![Nuget package](https://img.shields.io/nuget/vpre/Tiktoken)](https://www.nuget.org/packages/Tiktoken/)
[![dotnet](https://github.com/tryAGI/Tiktoken/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/tryAGI/Tiktoken/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/github/license/tryAGI/Tiktoken)](https://github.com/tryAGI/Tiktoken/blob/main/LICENSE.txt)
[![Discord](https://img.shields.io/discord/1115206893015662663?label=Discord&logo=discord&logoColor=white&color=d82679)](https://discord.gg/Ca2xhfBf3v)

Due to the lack of a C# version of `cl100k_base` encoding (gpt-3.5-turbo), I have implemented a basic solution with encoding and decoding methods based on the official Rust implementation.

Currently, `cl100k_base` `p50k_base` has been implemented. Other encodings will be added in future submissions. If you encounter any issues or have questions, please feel free to submit them on the `lssues`."

### Usage
```csharp
using Tiktoken;

var encoding = Encoding.ForModel("gpt-3.5-turbo");
var tokens = encoding.Encode("hello world"); // [15339, 1917]
var text = encoding.Decode(tokens); // hello world

var encoding = Encoding.Get("cl100k_base");
var tokens = encoding.Encode("hello world"); // [15339, 1917]
var text = encoding.Decode(tokens); // hello world
```

**When using a new encoder for the first time, the required tiktoken files for the encoder will be downloaded from the internet. This may take some time.** Once the download is successful, subsequent uses will not require downloading again. You can set `TikToken.PBEFileDirectory` before using the encoder to modify the storage path of the downloaded tiktoken files, or you can pre-download the files to avoid network issues causing download failures.

**Why are the tiktoken files not integrated into the package?** On one hand, this would make the package size larger. On the other hand, I want to stay as consistent as possible with OpenAI's official Python code.

Below are the file download links:
[p50k_base.tiktoken](https://openaipublic.blob.core.windows.net/encodings/p50k_base.tiktoken)
[cl100k_base.tiktoken](https://openaipublic.blob.core.windows.net/encodings/cl100k_base.tiktoken)

## Efficiency Comparison

I noticed that some users would like to get a comparison of efficiency. Here, I use SharpToken as the basic comparison, with the encoder cl100k_base, on the .Net 6.0 in Debug mode.
* TiktokenSharp Version: 1.0.5 
* SharpToken Version: 1.0.28

### CPU

<details> 
<summary>Code：</summary>

```csharp
const string kLongText = "King Lear, one of Shakespeare's darkest and most savage plays, tells the story of the foolish and Job-like Lear, who divides his kingdom, as he does his affections, according to vanity and whim. Lear’s failure as a father engulfs himself and his world in turmoil and tragedy.";

static async Task SpeedTiktokenSharp()
{
    TikToken tikToken = TikToken.GetEncoding("cl100k_base");
    Stopwatch stopwatch = new Stopwatch();
    stopwatch.Start();

    for (int i = 0; i < 10000; i++) 
    {
        var encoded = tikToken.Encode(kLongText);
        var decoded = tikToken.Decode(encoded);
    }

    stopwatch.Stop();
    TimeSpan timespan = stopwatch.Elapsed;
    double milliseconds = timespan.TotalMilliseconds;
    Console.WriteLine($"SpeedTiktokenSharp = {milliseconds} ms");
}

static async Task SpeedSharpToken()
{
    var encoding = GptEncoding.GetEncoding("cl100k_base");

    Stopwatch stopwatch = new Stopwatch();
    stopwatch.Start();   

    for (int i = 0; i < 10000; i++) 
    {
        var encoded = encoding.Encode(kLongText);
        var decoded = encoding.Decode(encoded);
    }

    stopwatch.Stop();
    TimeSpan timespan = stopwatch.Elapsed;
    double milliseconds = timespan.TotalMilliseconds;
    Console.WriteLine($"SpeedSharpToken = {milliseconds} ms");

}
```
  
</details>
TiktokenSharp is approximately 57% faster than SharpToken.

* Speed`TiktokenSharp` = 570.1206 ms
* Speed`SharpToken` = 1312.8812 ms

### Memory
<details> <summary>Image：</summary>
  
![20230509125926](https://user-images.githubusercontent.com/4475018/236998921-d380899e-9b66-43c9-af66-f02bf8c2c6e5.png)
![20230509130021](https://user-images.githubusercontent.com/4475018/236998944-eb1d1cf6-65b4-4669-9160-a8fc74e0d4c9.png)
  
</details>

TiktokenSharp has approximately 26% less memory usage than SharpToken.


## Update

### 1.0.5 20230508
* New support for .Net Standard 2.0 has been added, making TiktokenSharp usable in the .Net Framework.

### 1.0.4 20230424
* Add method TikToken.GetEncoding(encodingName).

### 1.0.3 20230321
* **GetEncodingSetting** now supports the model of **gpt-4** and also allows for encoding names to be directly passed in.

### 1.0.2 20230317
* add method **TikToken.PBEFileDirectory** to allow for custom storage directory of bpe files. the path needs to be set before **TikToken.EncodingForModel()**.

### 1.0.1 20230313
* p50k_base encoding algorithm that supports the text-davinci-003 model.


