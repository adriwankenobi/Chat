# Chat
Small chat app to test .NET Service Fabric

Hosted in Azure. Endpoint:
```
http://ngcluster.northeurope.cloudapp.azure.com:8080/
```

# Test client

I made a small client in Python to test the chat app
```sh
$ ./ChatClient/chatclient.py <server>
```

# Useful commands for testing the API

Register
```sh
$ curl -X PUT -s -i http://<server>:8080/api/users -H "Content-Type: application/json" -d "{\"name\":\"adrian\"}"
```

Login
```sh
$ curl -X PUT -s -i http://<server>:8080/api/users?id=<user_id> -H "Content-Type: application/json" -H "Content-Length: 0"
```

Get all rooms
```sh
$ curl -X GET -s -i http://<server>:8080/api/rooms?id=<user_id> -H "Accept: application/json"
```

Create a room
```sh
$ curl -X PUT -s -i http://<server>:8080/api/rooms?id=<user_id> -H "Content-Type: application/json" -d "{\"name\":\"friends\"}"
```

Join a room
```sh
$ curl -X PUT -s -i http://<server>:8080/api/rooms/<room_id>?id=<user_id> -H "Content-Type: application/json" -H "Content-Length: 0"
```

Leave a room
```sh
$ curl -X DELETE -s -i http://<server>:8080/api/rooms/<room_id>?id=<user_id> -H "Accept: application/json"
```

Get all messages from a room
```sh
$ curl -X GET -s -i http://<server>:8080/api/rooms/<room_id>?id=<user_id> -H "Accept: application/json"
```

Post a message to a room
```sh
$ curl -X POST -s -i http://<server>:8080/api/rooms/<room_id>?id=<user_id> -H "Content-Type: application/json" -d "{\"content\":\"Hello world\"}"
```

Logout
```sh
$ curl -X DELETE -s -i http://desktop-605aq1e:8080/api/users?id=<user_id> -H "Accept: application/json"
```

# TODO
HTTPS support

Add authentication and request validation in middle layers using annotations

Unit tests for API calls

Broadcast messages to connected users on posting