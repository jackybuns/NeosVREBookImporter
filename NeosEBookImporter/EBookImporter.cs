using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using BaseX;
using EpubSharp;
using FrooxEngine;
using FrooxEngine.UIX;

namespace NeosEBookImporter
{
    [Category("EBook Importer")]
    public class EBookImporter : Component , ICustomInspector
    {
        private const string DynVarSpaceName = "EBook";
        private const string DynVarTitleName = DynVarSpaceName + "/Title";
        private const string DynVarAuthorCountName = DynVarSpaceName + "/AuthorCount";
        private const string DynVarAuthorName = DynVarSpaceName + "/Author"; // will be + AuthorNr for multiple authors
        private const string DynVarChapterCountName = DynVarSpaceName + "/ChapterCount";
        private const string DynVarChaterName = DynVarSpaceName + "/Chapter"; // will be + AuthorNr for multiple authors
        private const string DynVarChaterTitleName = DynVarSpaceName + "/ChapterTitle"; // will be + AuthorNr for multiple authors

        public readonly Sync<string> EBookPath;
        public readonly Sync<bool> Recursive;
        
        private Text output;

        protected override void OnAttach()
        {
            base.OnAttach();
            //Todo: remove
            EBookPath.Value = "D:\\epubs\\Frankenstein.epub";
        }

        public void BuildInspectorUI(UIBuilder ui)
        {
            WorkerInspector.BuildInspectorUI((Worker)this, ui);
            ui.Button("import EPUB", (button, data) => { ImportEPUB(); });
            output = ui.Text("", true, Alignment.MiddleLeft, false);
            Message("Ready for import...");
        }

        private void Message(string message)
        {
            if (output != null)
            {
                output.Content.Value = message;
            }
        }

        private void AttachDynVar<T>(Slot slot, string name, T content)
        {
            var dynvar = slot.AttachComponent<DynamicValueVariable<T>>();
            dynvar.VariableName.Value = name;
            dynvar.Value.Value = content;
        }

        private void ImportEPUB()
        {
            // clear children
            foreach (var slotChild in Slot.Children)
            {
                slotChild.Destroy();
            }

            var filesToImport = new List<string>();
            var fileAttributes = File.GetAttributes(EBookPath.Value);

            if (fileAttributes.HasFlag(FileAttributes.Directory))
            {
                var files = Directory.GetFiles(EBookPath.Value, "*.epub",
                    Recursive.Value ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                filesToImport.AddRange(files);
            }
            else
            {
                filesToImport.Add(EBookPath.Value);
            }

            Message("Starting import...");
            //World.AddSlot(this.Slot, "EPUB");
            foreach (var file in filesToImport)
            {
                try
                {
                    if (!File.Exists(file))
                    {
                        Message("File could not be found!");
                        return;
                    }

                    EpubBook book = EpubReader.Read(file);

                    var bookSlot = Slot.AddSlot(book.Title);
                    var grabbable = bookSlot.AttachComponent<Grabbable>();
                    grabbable.Scalable.Value = true;
                    bookSlot.AttachComponent<ObjectRoot>();
                    var license = bookSlot.AttachComponent<License>();
                    license.CreditString.Value = "Imported using the NeosVREBookImporter plugin";
                    bookSlot.Tag = "ebook";

                    var snapper = bookSlot.AttachComponent<Snapper>();
                    snapper.Keywords.Add("ebook");

                    var dynVarsSlot = bookSlot.AddSlot("Dynamic Variables");
                    AttachDynVar(dynVarsSlot, DynVarTitleName, book.Title);

                    int authorCount = book.Authors.Count();
                    AttachDynVar(dynVarsSlot, DynVarAuthorCountName, authorCount);


                    int i = 0;
                    foreach (var bookAuthor in book.Authors)
                    {
                        AttachDynVar(dynVarsSlot, DynVarAuthorName + i, bookAuthor);
                        i++;
                    }

                    // Add chapters
                    var chaptersSlot = bookSlot.AddSlot("Chapters");
                    var chapterCount = AddChapters(book, chaptersSlot, book.TableOfContents);

                    AttachDynVar(chaptersSlot, DynVarChapterCountName, chapterCount);

                    CreateVisual(bookSlot, book.Title);

                    Message("Yay done!");
                }
                catch (FileNotFoundException e)
                {
                    Message("EPUB could not be found!");
                    Slot.AddSlot("Error message in tag").Tag = e.Message;
                }
                catch (IOException e)
                {
                    Message("EPUB could not be read!");
                    Slot.AddSlot("Error message in tag").Tag = e.Message;
                }
                catch (XmlException e)
                {
                    Message("Error occurred while parsing chapter html!");
                    Slot.AddSlot("Error message in tag").Tag = e.Message;
                }
                catch (Exception e)
                {
                    Message("An unknown error occurred while importing");
                    Slot.AddSlot("Error message in tag").Tag = e.Message;
                }
            }
        }

        private int AddChapters(EpubBook book, Slot chaptersSlot, IList<EpubChapter> chapters, int chapterCount = 0)
        {
            if(chapters.Count == 0)
                return 0;

            for (var i = 0; i < chapters.Count; i++, chapterCount++)
            {
                var chap = chapters[i];
                var chapterSlot = chaptersSlot.AddSlot($"Chapter {chapterCount}");
                AttachDynVar(chapterSlot, DynVarChaterTitleName+chapterCount, chap.Title);

                var text = book.Resources.Html.FirstOrDefault(x => x.FileName == chap.FileName);

                AttachDynVar(chapterSlot, DynVarChaterName+chapterCount, ParseChapterHtml(text.TextContent));
                
                // recurse all subchapters
                if (chap.SubChapters.Count > 0)
                {
                    chapterCount = AddChapters(book, chaptersSlot, chap.SubChapters, chapterCount);
                }
            }

            return chapterCount;
        }

        private string ParseChapterHtml(string html)
        {
            var doc = new XmlDocument();
            doc.LoadXml(html);

            var paragraphs = doc.GetElementsByTagName("p");
            StringBuilder sb = new StringBuilder();
            /*
            if (paragraphs.Count > 0)
            {
                for (int j = 0; j < paragraphs.Count; j++)
                {
                    var item = paragraphs.Item(j);
                    if (item != null)
                    {
                        sb.AppendLine(item.InnerText);
                        sb.AppendLine();
                    }
                }
            }
            else
            {
                var bodies = doc.GetElementsByTagName("body");
                for (int j = 0; j < bodies.Count; j++)
                {
                    var children = bodies[j].ChildNodes;
                    for (int k = 0; k < children.Count; k++)
                    {
                        sb.AppendLine(children[k].InnerText);
                        sb.AppendLine();
                    }
                }
            }
            */
            var bodies = doc.GetElementsByTagName("body");
            for (int j = 0; j < bodies.Count; j++)
            {
                ParseXMLNode(bodies.Item(j),sb);
            }

            return sb.ToString();
        }

        private void ParseXMLNode(XmlNode node, StringBuilder sb)
        {
            if (node.Name == "p" || node.Name.StartsWith("h"))
            {
                sb.AppendLine(node.InnerText);
                sb.AppendLine();
            }
            else
            {
                if (!node.HasChildNodes)
                {
                    sb.Append(node.InnerText).Append(" ");
                }
                else
                {
                    for (int i = 0; i < node.ChildNodes.Count; i++)
                    {
                        ParseXMLNode(node.ChildNodes.Item(i), sb);
                    }
                }
            }
        
        }

        private void CreateVisual(Slot parent, string title)
        {
            var visual = parent.AddSlot("Visual");
            var box = visual.AttachComponent<BoxMesh>();
            var meshRenderer = visual.AttachComponent<MeshRenderer>();
            var collider = visual.AttachComponent<BoxCollider>();
            meshRenderer.Mesh.Target = box;
            var size = new float3(0.18f, 0.24f, 0.02f);
            box.Size.Value = size;
            collider.Size.Value = size;

            var material = visual.AttachComponent<PBS_Metallic>();
            material.AlbedoColor.Value = color.Cyan;
            meshRenderer.Material.Value = material.ReferenceID;

            // create text
            var textSlot = visual.AddSlot("Text");
            var text = textSlot.AttachComponent<TextRenderer>();
            text.Text.Value = title;
            text.Align = Alignment.MiddleCenter;
            text.BoundsSize.Value = new float2(size.x, size.z);
            text.Bounded.Value = true;
            textSlot.LocalPosition = new float3(0f, 0f, -0.011f);
            text.Size.Value = 0.1f;
            text.Color.Value = color.Black;
        }
    }
}
