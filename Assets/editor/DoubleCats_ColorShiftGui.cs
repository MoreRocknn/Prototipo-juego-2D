using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DoubleCats_ColorShiftGui : ShaderGUI
{
    MaterialProperty _ZWrite;

    MaterialProperty _ZTest;

    MaterialProperty _Cull;

    MaterialProperty _mask;

    MaterialProperty _Base_alpha;

    MaterialProperty _shift;

    MaterialProperty _scale;

    MaterialProperty _mask2;

    MaterialProperty _noise_mask_uv;

    MaterialProperty _custom1_x_control_shift;

    MaterialProperty _custom1_zw_move_uv;

    MaterialProperty _X_speed;

    MaterialProperty _Y_speed;

    MaterialProperty _speed;

    public override void OnGUI(
        MaterialEditor materialEditor,
        MaterialProperty[] properties
    )
    {
        EditorGUILayout.LabelField("Glitch Split", EditorStyles.miniButton);


#region [Base Texture]

        EditorGUILayout.LabelField("Basic part", EditorStyles.miniButton);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        _mask = FindProperty("_mask", properties);
        materialEditor.TextureProperty(_mask, "Mask");

        _Base_alpha = FindProperty("_Base_alpha", properties);
        materialEditor.ShaderProperty(_Base_alpha, "Base_alpha");

        _shift = FindProperty("_shift", properties);
        materialEditor.ShaderProperty(_shift, "Glitch Split power");

        _scale = FindProperty("_scale", properties);
        materialEditor.ShaderProperty(_scale, "Glitch Split constant (0_1)");

        _custom1_x_control_shift =
            FindProperty("_custom1_x_control_shift", properties);
        materialEditor
            .ShaderProperty(_custom1_x_control_shift,
            "Use custom1_X to control Glitch Split power");
        EditorGUILayout.EndVertical();
#endregion



#region [Mask Texture]

        EditorGUILayout.LabelField("Mask part", EditorStyles.miniButton);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        _mask2 = FindProperty("_mask2", properties);

        materialEditor
            .TexturePropertySingleLine(new GUIContent("Noise Tex"), _mask2);

        _noise_mask_uv = FindProperty("_noise_mask_uv", properties);
        materialEditor.ShaderProperty(_noise_mask_uv, "Noise uv");

        _X_speed = FindProperty("_X_speed", properties);
        materialEditor.ShaderProperty(_X_speed, "X_speed");

        _Y_speed = FindProperty("_Y_speed", properties);
        materialEditor.ShaderProperty(_Y_speed, "Y_speed");

        _speed = FindProperty("_speed", properties);
        materialEditor.ShaderProperty(_speed, "speed");

        _custom1_zw_move_uv = FindProperty("_custom1_zw_move_uv", properties);
        materialEditor
            .ShaderProperty(_custom1_zw_move_uv, "use custom1_zw move noise uv");

        EditorGUILayout.EndVertical();
#endregion



#region [Extra settings]
        EditorGUILayout.LabelField("Extra settings", EditorStyles.miniButton);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        _ZWrite = FindProperty("_ZWrite", properties);
        materialEditor.ShaderProperty(_ZWrite, "ZWrite");

        _ZTest = FindProperty("_ZTest", properties);
        materialEditor.ShaderProperty(_ZTest, "ZTest");

        _Cull = FindProperty("_Cull", properties);
        materialEditor.ShaderProperty(_Cull, "Cull");

        // float _SaveValue01x=showdark?1:0;
        // float _SaveValue01y=showdark?1:0;
        // Vector4 _SaveValue01=new Vector4(_SaveValue01x,0,0,0);
        // _SaveValue.vectorValue=_SaveValue01;
        materialEditor.RenderQueueField();
        EditorGUILayout.EndVertical();
#endregion



#region [Precautions]
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout
            .LabelField("Warning!!!This shader is not performance optimized at all ",
            EditorStyles.miniButton);
        EditorGUILayout
            .LabelField("Author:DoubleCats",
            EditorStyles.miniButton);
        EditorGUILayout
            .LabelField("https://twitter.com/doublecats1", EditorStyles.miniButton);

        EditorGUILayout.EndVertical();


#endregion
    }
}
