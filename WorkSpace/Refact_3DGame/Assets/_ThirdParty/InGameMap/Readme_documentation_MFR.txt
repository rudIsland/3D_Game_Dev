
If you need much better texture resolution for HDRP, here you have it. We had to separate it from the package due to asset store submission requirements.

HIGH RESOLUTION TEKSTURES
https://bit.ly/3Yilblo

You need to replace the textures from the Assets\ScansFactory\MedievalRuins\Common\Textures

All rendering pipelines are in folders:

HDRP
Assets\ScansFactory\MedievalRuins\HDRP

URP
Assets\ScansFactory\MedievalRuins\URP

Builtin
Assets\ScansFactory\MedievalRuins\BuiltIn

If the entire scene is pink, check if the correct rendering pipeline is installed in the project.
Also, make sure to set the correct rendering pipeline asset in both the Quality settings and the Graphics settings.
They can be found in the following folders:

HDRP
Assets\ScansFactory\MedievalRuins\HDRP\Demo\HDRP_Settings\SF_HighFidelityHDRPAsset.asset

Builtin:
To use our post-processing in the Built-in Render Pipeline, you need to download the Post Processing package from the Package Manager.
Folder for attaching post-processing:
Assets\ScansFactory\MedievalRuins\BuiltIn\Demo\MedievalRuins_01_P_Profiles\Postprocess_Night_BuiltIn_Profile.asset
To ensure the Built-in settings work correctly, we recommend starting a clean project with Unity's default Built-in settings.
Remember to add the “PostProcessing” layer in Layers, in slot 6.

If the tips of the plants or water appear silver, reduce the Intensity Multiplier under Lighting ? Environment ? Environment Reflections to 0.2

If shadows are not visible in Built-in, please set the Shadow Distance to 150 in Project Settings.

If everything works well and you’d like to support us, please consider leaving a review — it really helps us continue creating more content for you!

For any questions or issues, feel free to contact us at support@scansfactory.com.
— Scans Factory Team