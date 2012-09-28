/*
	Yelo: Open Sauce SDK
		Halo 1 (CE) Edition

	See license\OpenSauce\Halo1_CE for specific license information
*/
#pragma once

#include "Memory/MemoryInterface.hpp"
#include <Blam/Halo1/BlamScriptingDefinitions.hpp>

// Utility function for checking input strings against a base string.
// eg, the base string of "weapon0" is "weapon".
// used in Players.Scripting.inl and Objects.Scripting.inl
#define _HS_UTIL_STRNCMP(value, str)	(strncmp(value,str,NUMBEROF(str)-1)==0)

namespace Yelo
{
	namespace TagGroups
	{
		struct scripting_block;
	};
};

namespace Yelo
{
	namespace Enums
	{
#define __SCRIPTLIBRARY_INCLUDE_ID __SCRIPTLIBRARY_ENUMERATIONS
#include "Game/ScriptLibrary.inl"
	};

	namespace Scripting
	{
		#pragma region hs_function
//////////////////////////////////////////////////////////////////////////
// Macro glue for declaring/defining a hs function which takes no arguments
#define DECLARE_HS_FUNCTION(name) extern Yelo::Scripting::hs_function_definition function_##name##_definition

//////////////////////////////////////////////////////////////////////////
// Macro glue for declaring/defining a hs function which takes various arguments for input
#define DECLARE_HS_FUNCTION_WITH_PARAMS(name) extern hs_function_definition function_##name##_definition

#define GET_HS_FUNCTION(name) Yelo::Scripting::function_##name##_definition


		const hs_function_definition* HSFunctionTable();

		const hs_function_definition& HSYeloFunction(int16 index);
		int32 HSYeloFunctionCount();
		#pragma endregion

		#pragma region hs_global
//////////////////////////////////////////////////////////////////////////
// Macro glue for declaring/defining a normal hs global
#define DECLARE_HS_GLOBAL(name) extern Yelo::Scripting::hs_global_definition global_##name##_definition

//////////////////////////////////////////////////////////////////////////
// Macro glue for declaring/defining an hs global with special flags
#define DECLARE_HS_GLOBAL_EX(name) extern Yelo::Scripting::hs_global_definition global_##name##_definition

//////////////////////////////////////////////////////////////////////////
// Macro glue for declaring/defining a hs global whose value is stored
// in the engine itself. Was useful in the case of 'gravity'
#define DECLARE_HS_GLOBAL2(name) extern Yelo::Scripting::hs_global_definition global_##name##_definition

#define GET_HS_GLOBAL(name) Yelo::Scripting::global_##name##_definition


		const hs_global_definition* HSExternalGlobals();

		const hs_global_definition& HSYeloGlobal(int16 index);
		int32 HSYeloGlobalCount();
		#pragma endregion


		void InitializeLibrary();
		void DisposeLibrary();

		// Unlocks or Locks all functions\globals that can't be used normally
		void AllowFullAccess(bool allow);

		// Set the function's (with no parameters) evaluator to one which 
		// does nothing and returns zero
		void NullifyScriptFunction(hs_function_definition& function);
		void NullifyScriptFunction(Enums::hs_function_enumeration function);
		// Set the function's (which expects parameters) evaluator to one 
		// which does nothing and returns zero
		void NullifyScriptFunctionWithParams(hs_function_definition& function);
		void NullifyScriptFunctionWithParams(Enums::hs_function_enumeration function);

		// Initialize the function's evaluator to one which we've defined 
		// in our code. Evaluator takes no parameters but may return a value.
		void InitializeScriptFunction(Enums::hs_function_enumeration function, 
			hs_yelo_function_proc proc);
		// Initialize the function's evaluator to one which we've defined 
		// in our code. Evaluator expects parameters and may return a value.
		void InitializeScriptFunctionWithParams(Enums::hs_function_enumeration function, 
			hs_yelo_function_with_params_proc proc);

#define YELO_INIT_SCRIPT_FUNCTION(function_index, proc)							Scripting::InitializeScriptFunction(function_index, proc)
#define YELO_INIT_SCRIPT_FUNCTION_WITH_PARAMS(function_index, proc)				Scripting::InitializeScriptFunctionWithParams(function_index, proc)
#if PLATFORM_IS_USER
	#define YELO_INIT_SCRIPT_FUNCTION_USER(function_index, proc)				YELO_INIT_SCRIPT_FUNCTION(function_index, proc)
	#define YELO_INIT_SCRIPT_FUNCTION_WITH_PARAMS_USER(function_index, proc)	YELO_INIT_SCRIPT_FUNCTION_WITH_PARAMS(function_index, proc)

	#define YELO_INIT_SCRIPT_FUNCTION_DEDI(function_index, proc)				Scripting::NullifyScriptFunction(function_index)
	#define YELO_INIT_SCRIPT_FUNCTION_WITH_PARAMS_DEDI(function_index, proc)	Scripting::NullifyScriptFunctionWithParams(function_index)
#else
	#define YELO_INIT_SCRIPT_FUNCTION_USER(function_index, proc)				Scripting::NullifyScriptFunction(function_index)
	#define YELO_INIT_SCRIPT_FUNCTION_WITH_PARAMS_USER(function_index, proc)	Scripting::NullifyScriptFunctionWithParams(function_index)

	#define YELO_INIT_SCRIPT_FUNCTION_DEDI(function_index, proc)				YELO_INIT_SCRIPT_FUNCTION(function_index, proc)
	#define YELO_INIT_SCRIPT_FUNCTION_WITH_PARAMS_DEDI(function_index, proc)	YELO_INIT_SCRIPT_FUNCTION_WITH_PARAMS(function_index, proc)
#endif

		// True if the script definitions in [data] match the functions/globals 
		// defined by Yelo's code.
		bool DefinitionsMatch(const TagGroups::scripting_block& data);

		// Interpret [data] as [type] data. Takes the [data].pointer and sets [data] to the dereferenced value.
		// If [data].pointer is NULL, then this sets [data] to [type]'s NONE equivalent.
		void UpdateTypeHolderFromPtrToData(TypeHolder& data, const Enums::hs_type type);
		// Interpret [ptr] as a [type] pointer. Takes [ptr], deferences it and sets [data] to the value.
		// [data] is 'const' as this doesn't modify the pointer, but the data which it points to.
		void UpdateTypeHolderDataFromPtr(const TypeHolder& data, const Enums::hs_type type, void* ptr);
	};
};