"""Package the Unity-rendered animation frames without altering their content."""
from pathlib import Path
from PIL import Image

project = Path(__file__).resolve().parents[1]
paths = sorted((project / "outputs/onion-reference/frames").glob("onion_*.png"))
if len(paths) != 75:
    raise RuntimeError(f"Expected 75 frames from OnionReferencePreview.Capture, found {len(paths)}")
frames = [Image.open(path).convert("RGB") for path in paths]
output = project / "docs/art/concepts/onion_reference_sv1_unity_animation_v1.gif"
frames[0].save(output, save_all=True, append_images=frames[1:],
               duration=[70,60,70]*25, loop=0, disposal=2, optimize=False)
for frame in frames:
    frame.close()
print(output)
