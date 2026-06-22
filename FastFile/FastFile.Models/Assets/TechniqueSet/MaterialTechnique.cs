using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.TechniqueSet;

public struct MaterialTechnique
{
    public XString Name;
    public UInt16 Flags;
    public UInt16 PassCount;
    public MaterialPass[] PassArray; //count = passCount
}