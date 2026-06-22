using Avalonia.Controls;
using FastFile.ModelsOLD.Assets.Weapons;
using FastFile.ModelsOLD.Assets.XModels;
using FastFile.ModelsOLD.Zone;
using System.Collections.Generic;

namespace UI.Views.Assets;

internal static class XModelPreviewHelper
{
    public static void Show(XModel model, Window? owner = null, string? contextName = null)
    {
        var window = new XModelViewerWindow(model, contextName);
        if (owner is not null)
        {
            window.Show(owner);
            return;
        }

        window.Show();
    }

    public static XModel? ResolveWeaponPreviewModel(WeaponVariantDef weapon)
    {
        var weaponDef = weapon.WeaponDef;
        if (weaponDef is null)
        {
            return null;
        }

        return ResolveFirstModel(weaponDef.gunXModel) ??
               ResolveModel(weaponDef.handXModel) ??
               ResolveFirstModel(weaponDef.WorldGunXModel) ??
               ResolveFirstModel(weaponDef.WorldModelPointers) ??
               ResolveModel(weaponDef.ProjectileModel);
    }

    public static string GetDisplayName(XModel model)
    {
        return string.IsNullOrWhiteSpace(model.GetDisplayName)
            ? "(unnamed xmodel)"
            : model.GetDisplayName;
    }

    private static XModel? ResolveFirstModel(XPointer<XPointer<XModel>[]>? pointerArray)
    {
        if (pointerArray is not { IsResolved: true, Value: { } pointers })
        {
            return null;
        }

        return ResolveFirstModel(pointers);
    }

    private static XModel? ResolveFirstModel(IEnumerable<XPointer<XModel>?> pointers)
    {
        foreach (var pointer in pointers)
        {
            var model = ResolveModel(pointer);
            if (model is not null)
            {
                return model;
            }
        }

        return null;
    }

    private static XModel? ResolveModel(XPointer<XModel>? pointer)
    {
        return pointer is { IsResolved: true, Value: { } model }
            ? model
            : null;
    }
}
