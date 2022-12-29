#data types
PACKET =                                   33
RPC =                                      34
PING =                                     35
PONG =                                     36

# packets identificators
ENCRYPTION_START_REQUEST =                 0
XOR_PUBLIC_KEY =                           1

PACKET_ENCRYPTED_CONNECTION_REQUEST =      11
PACKET_TRANSFORM_SYNC =                    12
PACKET_RIGIDBODY_SYNC =                    13
PACKET_GET_ROOM_LIST_REQUEST =             14
PACKET_ROOM_LIST =                         15
PACKET_JOIN_ROOM_REQUEST =                 16
PACKET_JOIN_ROOM_REQUEST_ACCEPTED =        17
PACKET_CREATE_ROOM_REQUEST =               18
PACKET_CREATE_ROOM_REQUEST_ACCEPTED =      19
PACKET_DISCONNECT =                        20
PACKET_CHAT =                              21
PACKET_CREATE_PLAYER_CLASS =               22
PACKET_REMOVE_PLAYER_CLASS =               23
PACKET_HOST_CHANGED =                      24
PACKET_INTERNAL_ERROR =                    25
PACKET_INSTANTIATE_GAMEOBJECT =            26
PACKET_DESTROY_GAMEOBJECT =                27
PACKET_GAMEOBJECT_TELEPORT =               28
PACKET_UPDATE_STREAM_ZONE =                29
PACKET_DESYNC_RIGIDBODY =                  30
PACKET_KICK_PLAYER =                       31
PACKET_CONSOLE_MESSAGE =                   32

#rpc targets
TARGET_ROOM =                              1
TARGET_HOST =                              2
TARGET_GLOBAL =                            3
TARGET_SERVER =                            4

#rpc data types
BYTE =                                     1
UINT16 =                                   2
INT16 =                                    3
UINT32 =                                   4
INT32 =                                    5
FLOAT =                                    6
STRING8 =                                  7
STRING16 =                                 8
BOOL =                                     9

#error codes
ERROR_SERVER_MAX_CONNECTIONS =             (1, "The server is full. Try connecting again")
ERROR_NICKNAME_TAKEN =                     (2, "The player with your nickname is already in the room. Players with the same nickname are not allowed")
ERROR_FULL_ROOM =                          (3, "The room you are trying to enter is filled with")
ERROR_ROOM_NOT_EXIST =                     (4, "The room you are trying to enter does not exist")
ERROR_MAX_PLAYER_PER_IP =                  (5, "Max players per IP")
ERROR_BAD_NICKNAME =                       (6, "Bad nickname. Allowed characters: 1-9, A-Z, A-Я")
ERROR_ROOM_WRONG_PASSWORD =                (7, "Wrong room password")
ERROR_ROOM_NAME_TAKEN =                    (8, "The name of the room to be created is occupied")