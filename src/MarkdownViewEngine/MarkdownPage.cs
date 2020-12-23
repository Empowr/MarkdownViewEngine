﻿using CommonMark;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Threading.Tasks;

namespace My.AspNetCore.Mvc.Markdown
{
    public class MarkdownPage : IMarkdownPage
    {
        private readonly IFileProvider _contentRootFileProvider;

        public MarkdownPage(IFileProvider contentRootFileProvider)
        {
            _contentRootFileProvider = contentRootFileProvider;
        }

        public IHtmlContent BodyContent { get; set; }

        public string Layout { get; set; }

        public dynamic Model { get; set; }

        public string Path { get; set; }

        public string Title { get; set; }

        public ViewContext ViewContext { get; set; }

        public async Task ExecuteAsync()
        {
            var fileInfo = _contentRootFileProvider.GetFileInfo(Path);
            string content;
            string markdown = string.Empty;
            using (var readStream = fileInfo.CreateReadStream())
            using (var reader = new StreamReader(readStream))
            {
                content = await reader.ReadToEndAsync();
            }
            if (content.StartsWith(MarkdownDirectives.Page, StringComparison.OrdinalIgnoreCase))
            {
                var newLineIndex = content.IndexOf(Environment.NewLine, MarkdownDirectives.Page.Length);
                var pageProperties = content.Substring(MarkdownDirectives.Page.Length, newLineIndex - MarkdownDirectives.Page.Length).Trim();
                var pageDirective = new MarkdownPageDirective();
                pageDirective.Process(pageProperties);
                Title = pageDirective.Title;
                Layout = pageDirective.Layout;
                markdown = content.Substring(content.IndexOf(Environment.NewLine));

                var modelIndex = content.IndexOf(MarkdownDirectives.Model, StringComparison.OrdinalIgnoreCase);
                if (modelIndex > -1)
                {
                    var modelProperties = content.Substring(modelIndex, content.IndexOf(Environment.NewLine, modelIndex) - modelIndex).Trim();
                    var modelDirective = new MarkdownModelDirective();
                    modelDirective.Process(modelProperties);
                    Model = modelDirective.Model;
                    markdown = content.Substring(content.IndexOf(Environment.NewLine, modelIndex));
                }
            }
            else if (content.StartsWith(MarkdownDirectives.Layout, StringComparison.OrdinalIgnoreCase))
            {
                var layoutProperties = content.Substring(MarkdownDirectives.Layout.Length).Trim();
                var layoutDirective = new MarkdownLayoutDirective();
                layoutDirective.Process(layoutProperties);
                Layout = Layout ?? layoutDirective.Name;
            }
            else if (content.StartsWith(MarkdownDirectives.Model, StringComparison.OrdinalIgnoreCase))
            {
                var newLineIndex = content.IndexOf(Environment.NewLine, MarkdownDirectives.Model.Length);
                var modelProperties = content.Substring(MarkdownDirectives.Model.Length, newLineIndex - MarkdownDirectives.Model.Length).Trim();
                var modelDirective = new MarkdownModelDirective();
                modelDirective.Process(modelProperties);
                Model = modelDirective.Model;
                markdown = content.Substring(content.IndexOf(Environment.NewLine));
            }
            else
            {
                markdown = content;
            }

            var html = CommonMarkConverter.Convert(markdown);
            BodyContent = new HtmlString(html);
        }
    }
}
