extern alias PIPE;
using PIPE::Microsoft.Xna.Framework.Content.Pipeline;
using System;

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
