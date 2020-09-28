#!/usr/bin/env python3
'''
Small client chat app for testing
'''

import sys
import requests


def usage():
    print('-------------------------------------------------------------------------')
    print("Usage: ./chatclient.py <server_url>")
    print("e.g: ./chatclient.py http://ngcluster.northeurope.cloudapp.azure.com:8080")
    print('-------------------------------------------------------------------------')


def main_menu():
    print('-------------------------------------------------------------------------')
    print('Choose an option')
    print('1 - Register')
    print('2 - Login')
    print('3 - Exit')
    print('-------------------------------------------------------------------------')


def register(url):
    print('What is your name?')
    for line in sys.stdin:
        user_name = line.rstrip()
        break
    r = requests.put(url = f'{url}/api/users', json={'name' : user_name}, headers={'Content-Type': 'application/json'})
    if r.status_code != 200:
        print(f'An error ocurred: {r.status_code}')
        exit()
    print(f'Welcome {user_name}!')
    return r.json()


def login(url):
    print('Give me your id')
    for line in sys.stdin:
        user_id = line.rstrip()
        break
    r = requests.put(url = f'{url}/api/users?id={user_id}', headers={'Content-Type': 'application/json', 'Content-Length': '0'})
    if r.status_code != 200:
        print(f'An error ocurred: {r.status_code}')
        exit()
    user = r.json()
    print(f'Welcome back {user["name"]}!')
    return user


def logout(url, user):
    r = requests.delete(url = f'{url}/api/users?id={user["id"]}', headers={'Accept': 'application/json'})
    if r.status_code != 200:
        print(f'An error ocurred: {r.status_code}')
        exit()
    print(f'Goodbye {user["name"]}!')


def print_user(user):
    print('-------------------------------------------------------------------------')
    print(f'Id: {user["id"]}')
    print(f'Name: {user["name"]}')
    print(f'Registered at: {user["registeredAt"]}')
    print('-------------------------------------------------------------------------')



def user_menu():
    print('-------------------------------------------------------------------------')
    print('Choose an option')
    print('1 - Show profile')
    print('2 - List all rooms')
    print('3 - Create a room')
    print('4 - Join a room')
    print('5 - Logout')
    print('-------------------------------------------------------------------------')


def get_all_rooms(url, user_id):
    r = requests.get(url = f'{url}/api/rooms?id={user_id}', headers={'Accept': 'application/json'})
    if r.status_code == 204:
        return []
    if r.status_code != 200:
        print(f'An error ocurred: {r.status_code}')
        exit()
    return r.json()


def create_room(url, user_id):
    print('What is the name of the new room?')
    for line in sys.stdin:
        room_name = line.rstrip()
        break
    r = requests.put(url = f'{url}/api/rooms?id={user_id}', json={'name' : room_name}, headers={'Content-Type': 'application/json'})
    if r.status_code != 200:
        print(f'An error ocurred: {r.status_code}')
        exit()
    return r.json()


def join_room(url, user_id):
    print('Which room you would like to join?')
    rooms = get_all_rooms(url, user_id)
    for i, r in enumerate(rooms, start=1):
        print(f'{i} - {r["name"]}')
    for line in sys.stdin:
        room_number = int(line.rstrip())-1
        room_id = rooms[room_number]["id"]
        break
    r = requests.put(url = f'{url}/api/rooms/{room_id}?id={user_id}', headers={'Content-Type': 'application/json', 'Content-Length': '0'})
    if r.status_code != 200:
        print(f'An error ocurred: {r.status_code}')
        exit()
    return rooms[room_number]


def print_room(room):
    print('-------------------------------------------------------------------------')
    print(f'Id: {room["id"]}')
    print(f'Name: {room["name"]}')
    print(f'Created at: {room["createdAt"]}')
    print(f'Members: {len(room["members"])}')
    print('-------------------------------------------------------------------------')


def messages_menu():
    print('-------------------------------------------------------------------------')
    print('Choose an option')
    print('1 - Post a message')
    print('2 - Leave room')
    print('-------------------------------------------------------------------------')


def get_all_messages(url, user_id, room_id):
    r = requests.get(url = f'{url}/api/rooms/{room_id}?id={user_id}', headers={'Accept': 'application/json'})
    if r.status_code == 204:
        return []
    if r.status_code != 200:
        print(f'An error ocurred: {r.status_code}')
        exit()
    return r.json()


def leave_room(url, user_id, room_id):
    r = requests.delete(url = f'{url}/api/rooms/{room_id}?id={user_id}', headers={'Accept': 'application/json'})
    if r.status_code != 200:
        print(f'An error ocurred: {r.status_code}')
        exit()


def post_message(url, user_id, room_id, content):
    r = requests.post(url = f'{url}/api/rooms/{room_id}?id={user_id}', json={'content' : content}, headers={'Content-Type': 'application/json'})
    if r.status_code != 200:
        print(f'An error ocurred: {r.status_code}')
        exit()
    return r.json()


def main():
    if len(sys.argv) < 2:
        usage()
        exit()

    url = sys.argv[1]
    user = None

    if user is None:
        main_menu()
        for line in sys.stdin:
            if '1' == line.rstrip():
                user = register(url)
                break
            if '2' == line.rstrip():
                user = login(url)
                break
            if '3' == line.rstrip():
                exit()

    user_menu()
    for line in sys.stdin:
        if '1' == line.rstrip():
            print_user(user)
        if '2' == line.rstrip():
            rooms = get_all_rooms(url, user["id"])
            if not rooms:
                print("No rooms available")
            else:
                for r in rooms:
                    print_room(r)
        if '3' == line.rstrip():
            room = create_room(url, user["id"])
            print_room(room)
        if '4' == line.rstrip():
            room = join_room(url, user["id"])
            user["rooms"].append(room["id"])
            print_room(room)
            msgs = get_all_messages(url, user["id"], room["id"])
            if not msgs:
                print("No messages available")
            else:
                for m in msgs:
                    print(f'{m["senderName"]}: {m["content"]}')

            messages_menu()
            for line in sys.stdin:
                if '1' == line.rstrip():
                    for line in sys.stdin:
                        content = line.rstrip()
                        break
                    post_message(url, user["id"], room["id"], content)
                if '2' == line.rstrip():
                    leave_room(url, user["id"], room["id"])
                    user["rooms"].remove(room["id"])
                    break

                messages_menu()

        if '5' == line.rstrip():
            logout(url, user)
            exit()

        user_menu()


if __name__ == '__main__':
    main()
