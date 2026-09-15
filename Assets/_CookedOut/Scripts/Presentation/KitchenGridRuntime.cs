using CookedOut.Domain;
using UnityEngine;

namespace CookedOut.Presentation
{
    public sealed class KitchenGridRuntime : MonoBehaviour
    {
        public KitchenGridDefinition Definition { get; private set; }

        public void Initialize(KitchenGridDefinition definition)
        {
            Definition = definition;
        }

        public Vector3 CellCenter(GridCoordinate cell, float y = 0f)
        {
            var point = Definition.Space.CellCenter(cell);
            return new Vector3(point.X, y, point.Z);
        }

        public GridCoordinate WorldToCell(Vector3 position)
        {
            return Definition.Space.WorldToCell(position.x, position.z);
        }
    }
}
