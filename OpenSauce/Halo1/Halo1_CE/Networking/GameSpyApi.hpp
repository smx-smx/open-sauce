/*
    Yelo: Open Sauce SDK
		Halo 1 (CE) Edition
    Copyright (C) 2005-2010  Kornner Studios (http://kornner.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
#pragma once

namespace Yelo
{
	namespace Enums
	{
		enum gamespy_qr_field
		{
			_gamespy_qr_field_reserved,
			_gamespy_qr_field_hostname,
			_gamespy_qr_field_gamename,
			_gamespy_qr_field_gamever,
			_gamespy_qr_field_hostport,
			_gamespy_qr_field_mapname,
			_gamespy_qr_field_gametype,
			_gamespy_qr_field_gamevariant,
			_gamespy_qr_field_numplayers,
			_gamespy_qr_field_numteams,
			_gamespy_qr_field_maxplayers,
			_gamespy_qr_field_gamemode,
			_gamespy_qr_field_teamplay,
			_gamespy_qr_field_fraglimit,
			_gamespy_qr_field_team_fraglimit,
			_gamespy_qr_field_timeelapsed,
			_gamespy_qr_field_timelimit,
			_gamespy_qr_field_roundtime,
			_gamespy_qr_field_roundelapsed,
			_gamespy_qr_field_password,
			_gamespy_qr_field_groupid,
			_gamespy_qr_field_player,
			_gamespy_qr_field_score,
			_gamespy_qr_field_skill,
			_gamespy_qr_field_ping,
			_gamespy_qr_field_team,
			_gamespy_qr_field_deaths,
			_gamespy_qr_field_pid,
			_gamespy_qr_field_team_t,
			_gamespy_qr_field_score_t,
			_gamespy_qr_field_reserved1,
			_gamespy_qr_field_reserved2,
			_gamespy_qr_field_reserved3,
			_gamespy_qr_field_reserved4,
			_gamespy_qr_field_reserved5,
			_gamespy_qr_field_reserved6,
			_gamespy_qr_field_reserved7,
			_gamespy_qr_field_reserved8,
			_gamespy_qr_field_reserved9,
			_gamespy_qr_field_reserved10,
			_gamespy_qr_field_reserved11,
			_gamespy_qr_field_reserved12,
			_gamespy_qr_field_reserved13,
			_gamespy_qr_field_reserved14,
			_gamespy_qr_field_reserved15,
			_gamespy_qr_field_reserved16,
			_gamespy_qr_field_reserved17,
			_gamespy_qr_field_reserved18,
			_gamespy_qr_field_reserved19,
			_gamespy_qr_field_reserved20,
			_gamespy_qr_field_reserved21,

			_gamespy_qr_field_dedicated,
			_gamespy_qr_field_player_flags,
			_gamespy_qr_field_game_flags,
			_gamespy_qr_field_game_classic,

			_gamespy_qr_field,
			_gamespy_qr_field_max_registered_keys = 254,
		}; BOOST_STATIC_ASSERT( _gamespy_qr_field <= _gamespy_qr_field_max_registered_keys );

		enum gamespy_connection_state : long_enum
		{
			GTI2AwaitingServerChallenge,
			GTI2AwaitingAcceptance,
			GTI2AwaitingClientChallenge,
			GTI2AwaitingClientResponse,
			GTI2AwaitingAcceptReject,
			GTI2Connected,
			GTI2Closing,
			GTI2Closed,
		};
	};

	namespace Networking
	{
		struct s_gamespy_buffer
		{
			byte* buffer;
			uint32 buffer_size;
			uint32 length;
		};

		struct s_gamespy_socket
		{
			SOCKET socket;
			in_addr address;
			int16 port; PAD16;
			void* connections;
			void* closedConnections;
			UNKNOWN_TYPE(int32); // 0x14
			UNKNOWN_TYPE(int32);
			void* connectAttemptCallback;
			void* socketErrorCallback;
			PAD32; // 0x24 pointer to a proc
			PAD32; // 0x28 pointer to a proc
			void* unrecongizedMessageCallback;
			void* user_data; // 0x30, engine treats this as s_transport_endpoint*
			int32 incomingBufferSize;
			int32 outgoingBufferSize;
			UNKNOWN_TYPE(int32); // 0x3C
			UNKNOWN_TYPE(int32); // 0x40, I believe I saw some code treat this as a s_transport_endpoint* ...
			UNKNOWN_TYPE(int32); // 0x44
		}; BOOST_STATIC_ASSERT( sizeof(s_gamespy_socket) == 0x48 );
		struct s_gamespy_connection
		{
			in_addr address;
			int16 port; PAD16;

			s_gamespy_socket* socket;
			Enums::gamespy_connection_state state;

			byte pad[0x150 - 0x10]; // TODO
		}; BOOST_STATIC_ASSERT( sizeof(s_gamespy_connection) == 0x150 );

		struct s_gamespy_qr_data // query/response
		{
			SOCKET heartbeat_socket;
			char game_name[64];
			char priv_key[64];
			char runtime_key[4];
			void* proc_server_key;
			void* proc_player_key;
			void* proc_team_key;
			void* proc_key_list;
			void* proc_player_team_count;
			void* proc_adderror;
			PAD32; // void* proc
			PAD32; // void* proc
			uint32 last_heartbeat_time;
			uint32 last_keepalive_time;
			long_enum listed_state;
			BOOL is_public;
			int32 query_port;
			int32 read_socket;
			UNKNOWN_TYPE(int32);
			sockaddr_in heartbeat_addr;
			void* proc_process_cdkey; // void (PLATFORM_API*)(char* buffer, size_t buffer_size, sockaddr* src_addr)
			int32 client_msg_keys[10];
			int32 client_msg_key_index;
			void* user_data;
		}; BOOST_STATIC_ASSERT( sizeof(s_gamespy_qr_data) == 0x108 );


		struct s_gamespy_client_node
		{
			struct s_gamespy_client* client;
			s_gamespy_client_node* next, * prev;
		};
		struct s_gamespy_client
		{
			int32 id;					// 0x0
			char cd_hash[33];			// 0x4
			PAD24;
			uint32 skey;				// 0x28, (GetTickCount ^ rand) & 0x3FFF
			int32 ip;
			uint32 sent_req_time;		// 0x30, GetTickCount
			int32 number_of_retries;	// 0x34
			long_enum state;			// 0x38, 0 = sent request, 1 = ok, 2 = not ok, 3 = done;
			PAD32;						// 0x3C, void* proc_unk
			void* authenticate_proc;	// 0x40
			char* errmsg;				// 0x44
			// \auth\\pid\%d\ch\%s\resp\%s\ip\%d\skey\%dd
			char* req_str;				// 0x48, malloc'd char*
			uint32 req_str_length;		// 0x4C
		}; BOOST_STATIC_ASSERT( sizeof(s_gamespy_client) == 0x50 );

		struct s_gamespy_product
		{
			int32 game_pid;
			s_gamespy_client_node clients;
		};


		// If this is a server, returns all the machines connected to this machine on a specific pid
		s_gamespy_product* GsProducts(); // [4]

		s_gamespy_client* GsGetClient(int32 client_id);
	};
};