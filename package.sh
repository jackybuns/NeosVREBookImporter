#!/bin/bash

7z a "NeosVREbookImporter_$1.zip" \
	README.md \
	EpubSharp_LICENSE.txt \
	LICENSE \
	NeosEBookImporter/bin/Release/HtmlAgilityPack.dll \
	NeosEBookImporter/bin/Release/EpubSharp.dll \
	NeosEBookImporter/bin/Release/NeosEBookImporter.dll
