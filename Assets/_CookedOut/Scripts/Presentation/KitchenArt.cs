using CookedOut.Domain;
using UnityEngine;

namespace CookedOut.Presentation
{
    public static class KitchenArtPalette
    {
        public static readonly Color FloorA = new Color(0.40f, 0.44f, 0.45f);
        public static readonly Color FloorB = new Color(0.47f, 0.51f, 0.52f);
        public static readonly Color Wall = new Color(0.68f, 0.72f, 0.72f);
        public static readonly Color Structure = new Color(0.16f, 0.075f, 0.18f);
        public static readonly Color Worktop = new Color(0.74f, 0.76f, 0.75f);
        public static readonly Color Wood = new Color(0.56f, 0.31f, 0.13f);
        public static readonly Color WoodDark = new Color(0.31f, 0.15f, 0.07f);
        public static readonly Color Container = new Color(0.84f, 0.75f, 0.58f);
        public static readonly Color BoardIvory = new Color(0.90f, 0.89f, 0.84f);
        public static readonly Color Teal = new Color(0.02f, 0.59f, 0.64f);
        public static readonly Color Ochre = new Color(0.92f, 0.50f, 0.08f);
        public static readonly Color FlameBlue = new Color(0.08f, 0.48f, 1.0f);
        public static readonly Color DeepInset = new Color(0.07f, 0.045f, 0.08f);
        public static readonly Color Steel = new Color(0.63f, 0.65f, 0.65f);
        public static readonly Color SteelLight = new Color(0.82f, 0.84f, 0.83f);
        public static readonly Color SteelDark = new Color(0.36f, 0.39f, 0.40f);
        public static readonly Color Lettuce = new Color(0.33f, 0.84f, 0.16f);
        public static readonly Color LeafGreen = new Color(0.16f, 0.56f, 0.12f);
        public static readonly Color LettuceLight = new Color(0.76f, 0.96f, 0.28f);
        public static readonly Color Carrot = new Color(1f, 0.35f, 0.035f);
        public static readonly Color CarrotGroove = new Color(0.86f, 0.16f, 0.018f);
        public static readonly Color OnionSkin = new Color(0.91f, 0.54f, 0.17f);
        public static readonly Color OnionFlesh = new Color(1f, 0.88f, 0.59f);
        public static readonly Color OnionHighlight = new Color(1f, 0.72f, 0.29f);
        public static readonly Color Soup = new Color(0.96f, 0.43f, 0.055f);
        public static readonly Color BeefRaw = new Color(0.72f, 0.18f, 0.16f);
        public static readonly Color BeefCooked = new Color(0.38f, 0.16f, 0.07f);
        public static readonly Color Burned = new Color(0.08f, 0.055f, 0.045f);
    }

    public static class KitchenStationArtFactory
    {
        public static Color BodyColor(string archetypeId)
        {
            if (archetypeId == KitchenArchetypeIds.DeliveryPoint)
            {
                return new Color(0.14f, 0.58f, 0.29f);
            }

            return KitchenArtPalette.Steel;
        }

        public static bool Decorate(string archetypeId, Transform parent, float cellSize)
        {
            if (KitchenProductionAssetFactory.TryInstantiateStation(
                    archetypeId,
                    parent,
                    cellSize,
                    out _))
            {
                return true;
            }

            if (archetypeId == KitchenArchetypeIds.DeliveryPoint)
            {
                AddPart("Delivery Target Ring", PrimitiveType.Cylinder, parent,
                    new Vector3(0f, 0.49f, 0f), new Vector3(cellSize * 0.38f, 0.04f, cellSize * 0.38f),
                    KitchenArtPalette.Ochre);
                return false;
            }

            if (archetypeId == KitchenArchetypeIds.TrashBin)
            {
                AddPart("Trash Opening", PrimitiveType.Cylinder, parent,
                    new Vector3(0f, 0.5f, 0f), new Vector3(cellSize * 0.35f, 0.05f, cellSize * 0.35f),
                    KitchenArtPalette.SteelDark);
                AddPart("Trash Inner", PrimitiveType.Cylinder, parent,
                    new Vector3(0f, 0.54f, 0f), new Vector3(cellSize * 0.25f, 0.025f, cellSize * 0.25f),
                    KitchenArtPalette.DeepInset);
                AddPart("Trash Accent", PrimitiveType.Cube, parent,
                    new Vector3(0f, 0.05f, -cellSize * 0.43f), new Vector3(cellSize * 0.34f, 0.16f, 0.05f),
                    KitchenArtPalette.Ochre);
                return false;
            }

            if (archetypeId == KitchenArchetypeIds.IngredientCrate)
            {
                AddPart("Stainless Produce Bin", PrimitiveType.Cube, parent,
                    new Vector3(0f, 0.02f, 0f), new Vector3(cellSize * 0.80f, 0.76f, cellSize * 0.76f),
                    KitchenArtPalette.Steel);
                for (var column = -1; column <= 1; column++)
                {
                    AddPart("Produce Bin Vent " + (column + 2), PrimitiveType.Cube, parent,
                        new Vector3(column * cellSize * 0.20f, 0.22f, -cellSize * 0.405f),
                        new Vector3(cellSize * 0.12f, 0.28f, 0.055f), KitchenArtPalette.SteelDark);
                }
                AddPart("Produce Bin Dark Interior", PrimitiveType.Cube, parent,
                    new Vector3(0f, 0.43f, 0f), new Vector3(cellSize * 0.62f, 0.12f, cellSize * 0.62f),
                    KitchenArtPalette.DeepInset);
                AddPart("Produce Bin Teal Rim", PrimitiveType.Cube, parent,
                    new Vector3(0f, 0.52f, 0f), new Vector3(cellSize * 0.84f, 0.08f, cellSize * 0.80f),
                    KitchenArtPalette.Teal);
                return false;
            }

            AddPart("Rolled Stainless Worktop", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.49f, 0f), new Vector3(cellSize * 0.80f, 0.10f, cellSize * 0.80f),
                KitchenArtPalette.SteelLight);
            AddDrawerFronts(parent, cellSize);

            switch (archetypeId)
            {
                case KitchenArchetypeIds.ChoppingBoard:
                    AddPart("Rounded Chopping Board", PrimitiveType.Cube, parent,
                        new Vector3(0f, 0.57f, 0f), new Vector3(cellSize * 0.62f, 0.08f, cellSize * 0.50f),
                        KitchenArtPalette.BoardIvory);
                    var knifeRoot = new GameObject("Knife Motion Root").transform;
                    knifeRoot.SetParent(parent, false);
                    knifeRoot.localPosition = new Vector3(-0.18f, 0.78f, -0.44f);
                    knifeRoot.localRotation = Quaternion.Euler(0f, 108f, 0f);
                    AddPart("Broad Chef Knife Blade", PrimitiveType.Cube, knifeRoot,
                        new Vector3(0.58f, 0f, 0f), new Vector3(cellSize * 0.30f, 0.055f, 0.30f),
                        new Color(0.76f, 0.79f, 0.83f), new Vector3(90f, 0f, 0f));
                    AddPart("Chef Knife Blade Highlight", PrimitiveType.Cube, knifeRoot,
                        new Vector3(0.60f, 0.032f, 0.03f), new Vector3(cellSize * 0.22f, 0.018f, 0.12f),
                        KitchenArtPalette.SteelLight, new Vector3(90f, 0f, 0f));
                    AddPart("Rounded Turquoise Knife Handle", PrimitiveType.Cube, knifeRoot,
                        new Vector3(0.08f, 0f, 0f), new Vector3(0.38f, 0.11f, 0.20f),
                        KitchenArtPalette.Teal, new Vector3(90f, 0f, 0f));
                    AddPart("Bright Knife Bolster", PrimitiveType.Cube, knifeRoot,
                        new Vector3(0.27f, 0f, 0f), new Vector3(0.11f, 0.12f, 0.23f),
                        KitchenArtPalette.SteelLight, new Vector3(90f, 0f, 0f));
                    var knifeDirection = new GameObject("Knife Blade Direction").transform;
                    knifeDirection.SetParent(knifeRoot, false);
                    knifeDirection.localPosition = new Vector3(0.76f, 0f, 0f);
                    break;
                case KitchenArchetypeIds.PotHeatSource:
                    AddPart("Pot Burner", PrimitiveType.Cylinder, parent,
                        new Vector3(0f, 0.56f, 0f), new Vector3(cellSize * 0.34f, 0.045f, cellSize * 0.34f),
                        KitchenArtPalette.DeepInset);
                    AddPart("Gas Flame Ring", PrimitiveType.Cylinder, parent,
                        new Vector3(0f, 0.615f, 0f), new Vector3(cellSize * 0.23f, 0.016f, cellSize * 0.23f),
                        KitchenArtPalette.FlameBlue);
                    AddPart("Burner Cap", PrimitiveType.Cylinder, parent,
                        new Vector3(0f, 0.64f, 0f), new Vector3(cellSize * 0.14f, 0.025f, cellSize * 0.14f),
                        KitchenArtPalette.SteelDark);
                    AddGasGrates(parent, cellSize);
                    AddPart("Heat Control", PrimitiveType.Cylinder, parent,
                        new Vector3(0f, 0.17f, -cellSize * 0.43f), new Vector3(0.16f, 0.07f, 0.16f),
                        KitchenArtPalette.Ochre, new Vector3(90f, 0f, 0f));
                    break;
                case KitchenArchetypeIds.PanHeatSource:
                    AddPart("Pan Burner", PrimitiveType.Cylinder, parent,
                        new Vector3(0f, 0.56f, 0f), new Vector3(cellSize * 0.34f, 0.045f, cellSize * 0.34f),
                        KitchenArtPalette.DeepInset);
                    AddPart("Pan Gas Flame Ring", PrimitiveType.Cylinder, parent,
                        new Vector3(0f, 0.615f, 0f), new Vector3(cellSize * 0.23f, 0.016f, cellSize * 0.23f),
                        KitchenArtPalette.FlameBlue);
                    AddPart("Pan Burner Cap", PrimitiveType.Cylinder, parent,
                        new Vector3(0f, 0.64f, 0f), new Vector3(cellSize * 0.14f, 0.025f, cellSize * 0.14f),
                        KitchenArtPalette.SteelDark);
                    AddGasGrates(parent, cellSize);
                    AddPart("Pan Heat Control", PrimitiveType.Cylinder, parent,
                        new Vector3(0f, 0.17f, -cellSize * 0.43f), new Vector3(0.16f, 0.07f, 0.16f),
                        KitchenArtPalette.Ochre, new Vector3(90f, 0f, 0f));
                    break;
                case KitchenArchetypeIds.AssemblyCounter:
                    AddPart("Assembly Recess", PrimitiveType.Cube, parent,
                        new Vector3(0f, 0.56f, 0f), new Vector3(cellSize * 0.48f, 0.06f, cellSize * 0.50f),
                        KitchenArtPalette.Worktop * 0.78f);
                    AddRailPair(parent, cellSize, KitchenArtPalette.Teal);
                    break;
                case KitchenArchetypeIds.ContainerDispenser:
                    AddPart("Container Rack Back", PrimitiveType.Cube, parent,
                        new Vector3(0f, 0.94f, cellSize * 0.31f),
                        new Vector3(cellSize * 0.66f, 0.72f, 0.12f), KitchenArtPalette.Teal);
                    AddPart("Container Pickup Bay", PrimitiveType.Cube, parent,
                        new Vector3(0f, 0.60f, -cellSize * 0.08f),
                        new Vector3(cellSize * 0.58f, 0.06f, cellSize * 0.48f), KitchenArtPalette.DeepInset);
                    AddPart("Container Pickup Lip", PrimitiveType.Cube, parent,
                        new Vector3(0f, 0.66f, -cellSize * 0.43f),
                        new Vector3(cellSize * 0.62f, 0.12f, 0.12f), KitchenArtPalette.Teal);
                    for (var stack = 0; stack < 3; stack++)
                    {
                        AddPart("Container Stack " + (stack + 1), PrimitiveType.Cube, parent,
                            new Vector3(0f, 0.67f + stack * 0.065f, -cellSize * 0.08f),
                            new Vector3(cellSize * 0.48f, 0.05f, cellSize * 0.38f),
                            KitchenArtPalette.Container);
                    }
                    AddPart("Container Open Lid Pict", PrimitiveType.Cube, parent,
                        new Vector3(0f, 1.08f, cellSize * 0.225f),
                        new Vector3(cellSize * 0.44f, 0.06f, 0.10f), KitchenArtPalette.BoardIvory,
                        new Vector3(-22f, 0f, 0f));
                    break;
                case KitchenArchetypeIds.ServingHatch:
                    AddPart("Serving Ledge", PrimitiveType.Cube, parent,
                        new Vector3(0f, 0.60f, -cellSize * 0.30f), new Vector3(cellSize * 0.64f, 0.12f, 0.18f),
                        KitchenArtPalette.Teal);
                    AddPart("Serving Arch Left", PrimitiveType.Cube, parent,
                        new Vector3(-cellSize * 0.31f, 0.90f, 0f), new Vector3(0.14f, 0.72f, 0.14f),
                        KitchenArtPalette.Ochre);
                    AddPart("Serving Arch Right", PrimitiveType.Cube, parent,
                        new Vector3(cellSize * 0.31f, 0.90f, 0f), new Vector3(0.14f, 0.72f, 0.14f),
                        KitchenArtPalette.Ochre);
                    AddPart("Serving Arch Top", PrimitiveType.Cube, parent,
                        new Vector3(0f, 1.22f, 0f), new Vector3(cellSize * 0.72f, 0.14f, 0.14f),
                        KitchenArtPalette.Ochre);
                    break;
                case KitchenArchetypeIds.BikeDock:
                    AddPart("Bike Load Surface", PrimitiveType.Cube, parent,
                        new Vector3(0f, 0.57f, 0f), new Vector3(cellSize * 0.68f, 0.08f, cellSize * 0.56f),
                        KitchenArtPalette.Teal);
                    break;
                case KitchenArchetypeIds.RecoveryBin:
                    AddPart("Recovery Slot", PrimitiveType.Cube, parent,
                        new Vector3(0f, 0.57f, 0f), new Vector3(cellSize * 0.42f, 0.07f, cellSize * 0.22f),
                        KitchenArtPalette.Teal);
                    AddPart("Recovery Accent Left", PrimitiveType.Cube, parent,
                        new Vector3(-0.18f, 0.63f, 0.04f), new Vector3(0.28f, 0.035f, 0.08f),
                        KitchenArtPalette.Ochre, new Vector3(0f, 32f, 0f));
                    AddPart("Recovery Accent Right", PrimitiveType.Cube, parent,
                        new Vector3(0.18f, 0.63f, -0.04f), new Vector3(0.28f, 0.035f, 0.08f),
                        KitchenArtPalette.Ochre, new Vector3(0f, 32f, 0f));
                    break;
            }

            return false;
        }

        private static void AddRailPair(Transform parent, float cellSize, Color color)
        {
            AddPart("Left Functional Rail", PrimitiveType.Cube, parent,
                new Vector3(-cellSize * 0.30f, 0.64f, 0f), new Vector3(0.14f, 0.22f, cellSize * 0.58f), color);
            AddPart("Right Functional Rail", PrimitiveType.Cube, parent,
                new Vector3(cellSize * 0.30f, 0.64f, 0f), new Vector3(0.14f, 0.22f, cellSize * 0.58f), color);
        }

        private static void AddGasGrates(Transform parent, float cellSize)
        {
            AddPart("Gas Grate Horizontal", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.66f, 0f), new Vector3(cellSize * 0.62f, 0.055f, 0.11f),
                KitchenArtPalette.DeepInset);
            AddPart("Gas Grate Vertical", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.66f, 0f), new Vector3(0.11f, 0.055f, cellSize * 0.62f),
                KitchenArtPalette.DeepInset);
        }

        private static void AddDrawerFronts(Transform parent, float cellSize)
        {
            AddPart("Large Upper Drawer", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.30f, -cellSize * 0.405f),
                new Vector3(cellSize * 0.65f, 0.22f, 0.055f), KitchenArtPalette.SteelLight);
            AddPart("Large Lower Drawer", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.02f, -cellSize * 0.405f),
                new Vector3(cellSize * 0.65f, 0.22f, 0.055f), KitchenArtPalette.SteelLight);
            AddPart("Upper Drawer Pull", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.35f, -cellSize * 0.445f),
                new Vector3(cellSize * 0.34f, 0.05f, 0.045f), KitchenArtPalette.SteelDark);
            AddPart("Lower Drawer Pull", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.07f, -cellSize * 0.445f),
                new Vector3(cellSize * 0.34f, 0.05f, 0.045f), KitchenArtPalette.SteelDark);
        }

        private static GameObject AddPart(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            Vector3? localEuler = null)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localEulerAngles = localEuler ?? Vector3.zero;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
            part.GetComponent<Renderer>().material = GrayboxMaterials.Create(color);
            return part;
        }
    }
}
