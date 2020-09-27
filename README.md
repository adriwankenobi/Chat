# Chat
Small chat app to test .NET Service Fabric

# Useful commands for testing the API

Get all rooms
```sh
$ curl -X GET -s -i http://<server>:8080/api/rooms -H "Accept: application/json"
```

Create a room
```sh
$ curl -X PUT -s -i http://<server>:8080/api/rooms -H "Content-Type: application/json" -d "{\"name\":\"<room_name>\"}"
```

Delete a room
```sh
$ curl -X DELETE -s -i http://<server>:8080/api/rooms/<room_id> -H "Accept: application/json"
```

Get all messages from a room
```sh
$ curl -X GET -s -i http://<server>:8080/api/rooms/<room_id> -H "Accept: application/json"
```

Post a message to a room
```sh
$ curl -X POST -s -i http://<server>:8080/api/rooms/<room_id> -H "Content-Type: application/json" -d "{\"senderId\":\"<sender_id>\",\"content\":\"Hello world!\"}"
```

# TODO
HTTPS support
Unit tests for API calls
Another layer for user connections