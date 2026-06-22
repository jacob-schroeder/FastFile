using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.TechniqueSet;

public struct MaterialTechniqueSet
{
    public const int TechniqueCount = 37;

    public XString Name;
    public MaterialWorldVertexFormat WorldVertexFormat;
    public XPointer<MaterialTechnique>[] Techniques;
}
