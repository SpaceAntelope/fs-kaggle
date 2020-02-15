# fs-kaggle
Minimalist jupyter-friendly Kaggle dataset downloader written if F#. Includes CLI and allows progress report customization.

*CLI instructions coming soon.*
# Installation
|Project|NuGet|dotnet cli|jupyter notebook|
| ------------- |:-------------:| ----- | --- |
| FsKaggle      | [![Nuget](https://img.shields.io/nuget/v/FsKaggle)](https://www.nuget.org/packages/FsKaggle/)|`dotnet add package FsKaggle`|`#r "nuget:FsKaggle"`|
| FsKaggle.CLI  | [![Nuget](https://img.shields.io/nuget/v/FsKaggle.CLI)](https://www.nuget.org/packages/FsKaggle.CLI/)| `dotnet tool install -g FsKaggle.CLI` ||

# Quickstart
If you're already setup with a kaggle account and the <code>kaggle.json</code> file is under *~/.kaggle*, you can just declare the name of the dataset and the dataset owner and sit back while the requested dataset zip is downloaded to the current directory.

<div class="alert alert-info" style="display:flex; align-items: center"><i class="fa fa-exclamation-triangle fa-2x" style="margin: .3em"></i> <span>You need to use the <code>FsKaggle.Interop</code> namespace to access the C# API, assuming you don't want to have to work around algebraic types and other F# dark magic.</span></div>

## CLI
Use `fskaggle --help` to see all available options and `fskaggle -x` to get a list of examples.
```bash
fskaggle dataset-owner dataset-name -f dataset-file.csv
```

## F#
```F#
open FsKaggle

{ Owner = "dataset-owner"
  Dataset = "dataset-name"
  Request = Filename "dataset-file.csv" 
  (* Use Request =  DatasetFile.All to get the full dataset *)  }
|> Kaggle.DownloadDatasetAsync  
|> Async.RunSynchronously
```
## C#   
```C#
using  FsKaggle.Interop; // !!

var options = 
    new  DatasetInfo 
    { 
        Owner = "dataset-owner", 
        Dataset = "dataset-name", 
        Request="dataset-file.csv" 
        /* Use Request = null (or just don't set it) to get the full dataset */
    };

await  Kaggle.DownloadDatasetAsync(options);
```

## Sample output:
dataset-file.zip [||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||| 100.00%] 1.94 of 1.94MB @ 667.05KB/s

# Further configuration and examples

* F# Notebook available [here](https://github.com/SpaceAntelope/fs-kaggle/blob/master/Notebooks/F%23%20Example.ipynb)
* C# Notebook available [here](https://github.com/SpaceAntelope/fs-kaggle/blob/master/Notebooks/C%23%20Example.ipynb)
