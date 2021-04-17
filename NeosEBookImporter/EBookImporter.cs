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

        public readonly Sync<string> EBookPath;
        
        private Text output;

        protected override void OnAttach()
        {
            base.OnAttach();
            EBookPath.Value = "D:\\Frankenstein.epub";
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

        private void ImportEPUB()
        {
            foreach (var slotChild in Slot.Children)
            {
                slotChild.Destroy();
            }

            Message("Starting import...");
            //World.AddSlot(this.Slot, "EPUB");
            try
            {
                EpubBook book = EpubReader.Read(EBookPath);

                var bookSlot = Slot.AddSlot("EBook");
                bookSlot.AttachComponent<Grabbable>();
                bookSlot.AttachComponent<ObjectRoot>();
                var license = bookSlot.AttachComponent<License>();
                license.CreditString.Value = "Imported using the NeosEBookImporter plugin";
                bookSlot.Tag = "ebook";

                var titleSlot = bookSlot.AddSlot(book.Title);
                var authorsSlot = titleSlot.AddSlot("Authors");

                foreach (var author in book.Authors)
                {
                    authorsSlot.AddSlot(author);
                }

                // Add chapters
                var chaptersSlot = titleSlot.AddSlot("Chapters");
                AddChapters(book, chaptersSlot, book.TableOfContents);

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

        private void AddChapters(EpubBook book, Slot chaptersSlot, IList<EpubChapter> chapters)
        {
            if(chapters.Count == 0)
                return;

            for (var i = 0; i < chapters.Count; i++)
            {
                var chap = chapters[i];
                var chapterSlot = chaptersSlot.AddSlot($"Chapter {i + 1}");
                chapterSlot.AddSlot("title").AddSlot(chap.Title);

                var text = book.Resources.Html.FirstOrDefault(x => x.FileName == chap.FileName);

                chapterSlot.AddSlot("Text").Tag = ParseChapterHtml(text.TextContent);
                
                // recurse all subchapters
                if (chap.SubChapters.Count > 0)
                {
                    var subChapter = chapterSlot.AddSlot("Subchapters");
                    AddChapters(book, subChapter, chap.SubChapters);
                }
            }

            
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
