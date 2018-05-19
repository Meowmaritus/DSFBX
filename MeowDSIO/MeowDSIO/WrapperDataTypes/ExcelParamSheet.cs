using MeowDSIO.DataFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.WrapperDataTypes
{
    public class ExcelParamSheet : WrapperData
    {
        public PARAM Param { get; private set; }
        public PARAMDEF ParamDef { get; private set; }



        public ExcelParamSheet(PARAM Param, PARAMDEF ParamDef)
            : base()
        {
            this.Param = Param;
            this.ParamDef = ParamDef;
        }

        protected override void ReloadContents()
        {
            //DataFile.Reload(Param);
            //DataFile.Reload(ParamDef);
        }

        protected override void ResaveContents()
        {
            //DataFile.Resave(Param);
            //DataFile.Resave(ParamDef);
        }

        protected override void Init()
        {
            
        }
    }
}
