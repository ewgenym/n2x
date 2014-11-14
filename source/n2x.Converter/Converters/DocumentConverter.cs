﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace n2x.Converter.Converters
{
    public abstract class DocumentConverter
    {
        public Document Convert(Document document)
        {
            var result = document;

            var converters = GetConverters();
            foreach (var converter in converters)
            {
                var root = result.GetSyntaxRootAsync().Result;
                var semanticModel = result.GetSemanticModelAsync().Result;

                var newRoot = converter.Convert(root, semanticModel);
                result = result.WithSyntaxRoot(newRoot.NormalizeWhitespace());
            }

            return result;
        }

        protected abstract IEnumerable<IConverter> GetConverters();
    }
}