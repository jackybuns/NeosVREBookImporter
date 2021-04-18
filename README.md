# NeosVREBookImporter

This is a plugin for NeosVR that lets you import EPUB e-books into a specific format that can be used to display them in the metaverse.

It uses the [EpubSharp](https://github.com/Asido/EpubSharp) library to import EPUBs, some books may not work.

# Legality disclaimer

**If you import ebooks into NeosVR and sync them to the cloud, you are only allowed to share them with other people if you actually have the right to do so.
If the book is in the public domain - and your local laws allow you to - you can share it freely in public folders or hand them to other people.
On the other hand, if the book is under copyright or other licsenses that prohibit distribution of said book, you are only allowed to use it for yourself and NOT share it with others.**

**No legal liability falls upon the developers of this tool, you are responsible for your own doings. 
The tools just provides the means to import ebooks, what you do with the ebooks is your responsibility.**

# How to use

 1. Download the necessary files from the reselse section to the right
 2. Put all the .dll files into your NeosVR installions Libraries folder
 3. Open the NeosVR Launcher and select the NeosEBookImporter.dll in the "Load Extra Libraries" section
 4. Start NeosVR
 5. Using a DevTooltip create a new empty objects
 6. Attach the ebook importer plugin in the "EBook Importer" section of the component selection
 7. Enter a valid local path to an epub file or a directory containing epub files
 8. If you recusively want to import all epubs of a folder hierarchy check the Recursive checkbox (this can take a while depending on the number of ebooks)

# How to build it yourself

 1. Check out the code or download it as a zip from Github
 2. Make sure you have Visual Studio 2019 and the .Net Framework 4.6.2 installed
 3. Open the NeosEBookImporter.sln file in Visual Studio
 5. Rebind the NeosVR dlls if you they are not found immediatelly. Check the [NeosVR Wiki](https://wiki.neos.com/Plugins) for more information.
 4. Click Build -> Build Solution
    * If you installed NeosVR on Steam to your C: drive the dlls are automatically copied there, so you just need to start the NeosVR Launcher
    * If you installed it somewhere else, copy all the dlls from the bin folder of the project to your NeosVR Libraries folder
    
# EBook NeosVR format description

The plugin creates an EBook in NeosVR in a certain format. Data is stored in Dynamic Variables for easy access. 
The DynVars all bind to the same DynVar space, but no Dynamic Variable Space is attached to the book. 
If you attach a DynVarSpace to you ebook reader and parent the book under it you have easy access to the variables.

DynVar Space: **EBook**

## Dynamic Variables

| Variable name | Description |
| --- | --- |
| **EBook/Title** | The ebook title |
| **EBook/AuthorCount** | The number of authors that wrote the book |
| **EBook/Author#** | A specific author. # is the 0 based index of the author. So the first author is Author0, the second one Author1, and so on. |
| **EBook/ChapterCount** | The number of chapters in the book |
| **EBook/Chapter#** | The contents of a specific chapter. # is also the 0 based index like the authors. |
| **EBook/ChapterTitle#** | The title of the specific chapter. # is the index again |
