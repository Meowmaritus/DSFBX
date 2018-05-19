using Microsoft.Xna.Framework.Content.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSFBX
{
    internal class DSFBXContentImporterContext : ContentImporterContext
    {
        public override void AddDependency(string filename)
        {
            throw new NotImplementedException();
        }

        ContentBuildLogger _logger = new DSFBXContentBuildLogger();

        public override ContentBuildLogger Logger => _logger;
        public override string OutputDirectory => "ContentImporterContext_out";
        public override string IntermediateDirectory => "ContentImporterContext_inter";
    }
}
