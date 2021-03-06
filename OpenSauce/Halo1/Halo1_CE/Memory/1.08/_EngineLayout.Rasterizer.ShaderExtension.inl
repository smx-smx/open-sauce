/*
	Yelo: Open Sauce SDK
		Halo 1 (CE) Edition

	See license\OpenSauce\Halo1_CE for specific license information
*/


//////////////////////////////////////////////////////////////////////////
// ShaderExtension.cpp
#if __EL_INCLUDE_FILE_ID == __EL_RASTERIZER_SHADEREXTENSION
	FUNC_PTR(RASTERIZER_MODEL_DRAW_INVERT_BACKFACE_NORMALS_CHECK_HOOK,		0x52E395, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_DRAW_INVERT_BACKFACE_NORMALS_CHECK_RETN,		0x52E39B, FUNC_PTR_NULL);

	FUNC_PTR(RASTERIZER_DX9_SHADERS_EFFECT_SHADERS_INITIALIZE__SPRINTF_CALL,0x533064, PTR_NULL);

	static cstring* K_VSH_COLLECTION_PATH_REFERENCES[] = {
		CAST_PTR(cstring*, 0x533ACE), CAST_PTR(cstring*, 0x533B69),
	};

	static void** K_PS_VERSION_ITERATOR_START[] = {
		CAST_PTR(void**, 0x532FF8), CAST_PTR(void**, 0x533522)
	};

#elif __EL_INCLUDE_FILE_ID == __EL_RASTERIZER_SHADEREXTENSION_MODEL
	static void** K_SHADER_USAGE_ID_ARRAY_REFERENCES[] = {
		CAST_PTR(void**, 0x52A14D), CAST_PTR(void**, 0x52A1C5), 
		CAST_PTR(void**, 0x52A244), CAST_PTR(void**, 0x52A2C4), 
		CAST_PTR(void**, 0x52A365), CAST_PTR(void**, 0x52A0F7)
	};

	ENGINE_PTR(int, RASTERIZER_MODEL_SHADER_LOAD_COUNT,				0x52A01E, PTR_NULL);
	ENGINE_PTR(int, RASTERIZER_MODEL_SHADER_LOAD_NO_INVERSE_COUNT,	0x52A023, PTR_NULL);

	FUNC_PTR(RASTERIZER_MODEL_ENVIRONMENT_NO_USAGE_ID_OFFSET_HOOK,	0x52A0E8, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_ENVIRONMENT_NO_USAGE_ID_OFFSET_RETN,	0x52A0F5, FUNC_PTR_NULL);

	FUNC_PTR(RASTERIZER_MODEL_NO_USAGE_ID_OFFSET_HOOK,				0x52A356, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_NO_USAGE_ID_OFFSET_RETN,				0x52A363, FUNC_PTR_NULL);


	static void** K_PIXEL_SHADER_REFERENCES_ENVIRONMENT_NO[] = {
		CAST_PTR(void**, 0x52A067), CAST_PTR(void**, 0x52A129)
	};
	static void** K_PIXEL_SHADER_REFERENCES_CHANGE_COLOR[] = {
		CAST_PTR(void**, 0x52A087), CAST_PTR(void**, 0x52A206)
	};
	static void** K_PIXEL_SHADER_REFERENCES_MULTIPURPOSE[] = {
		CAST_PTR(void**, 0x52A097), CAST_PTR(void**, 0x52A286)
	};
	static void** K_PIXEL_SHADER_REFERENCES_NO[] = {
		CAST_PTR(void**, 0x52A0BD), CAST_PTR(void**, 0x52A395)
	};
	static void** K_PIXEL_SHADER_REFERENCES_REFLECTION[] = {
		CAST_PTR(void**, 0x52A0A7), CAST_PTR(void**, 0x52A306)
	};
	static void** K_PIXEL_SHADER_REFERENCES_SELF_ILLUMINATION[] = {
		CAST_PTR(void**, 0x52A077), CAST_PTR(void**, 0x52A187)
	};


	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_ENVIRONMENT_NO_HOOK,			0x52B8FA, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_ENVIRONMENT_NO_RETN,			0x52B901, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_ENVIRONMENT_NO_INV_HOOK,		0x52B8EA, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_ENVIRONMENT_NO_INV_RETN,		0x52B8F1, FUNC_PTR_NULL);

	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_CHANGE_COLOR_HOOK,			0x52AD21, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_CHANGE_COLOR_RETN,			0x52AD28, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_CHANGE_COLOR_INV_HOOK,		0x52AD18, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_CHANGE_COLOR_INV_RETN,		0x52AD1F, FUNC_PTR_NULL);

	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_MULTIPURPOSE_HOOK,			0x52AD33, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_MULTIPURPOSE_RETN,			0x52AD3A, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_MULTIPURPOSE_INV_HOOK,		0x52AD2A, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_MULTIPURPOSE_INV_RETN,		0x52AD31, FUNC_PTR_NULL);

	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_NO_HOOK,						0x52ACEB, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_NO_RETN,						0x52ACF2, FUNC_PTR_NULL);

	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_REFLECTION_HOOK,				0x52AD0F, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_REFLECTION_RETN,				0x52AD16, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_REFLECTION_INV_HOOK,			0x52AD06, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_REFLECTION_INV_RETN,			0x52AD0D, FUNC_PTR_NULL);

	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_SELF_ILLUMINATION_HOOK,		0x52ACFD, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_SELF_ILLUMINATION_RETN,		0x52AD04, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_SELF_ILLUMINATION_INV_HOOK,	0x52ACF4, FUNC_PTR_NULL);
	FUNC_PTR(RASTERIZER_MODEL_PS_INDEX_SELF_ILLUMINATION_INV_RETN,	0x52ACFB, FUNC_PTR_NULL);

#elif __EL_INCLUDE_FILE_ID == __EL_RASTERIZER_SHADEREXTENSION_ENVIRONMENT

#else
	#error Undefined engine layout include for: __EL_RASTERIZER_SHADEREXTENSION
#endif