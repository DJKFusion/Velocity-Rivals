"""Private two-player room relay. Deploy behind HTTPS/WSS; no game authority lives here."""
import asyncio
from collections import Counter, deque
import json
import os
import secrets
import time
from dataclasses import dataclass, field

from websockets.asyncio.server import serve
from websockets.exceptions import ConnectionClosed
from websockets.http11 import Response
from websockets.datastructures import Headers

PROTOCOL = 3
ALPHABET = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'
HOST_MESSAGES = {'lobby', 'start', 'state', 'ping', 'pong'}
GUEST_MESSAGES = {'hello', 'loaded', 'input', 'ping', 'pong'}


@dataclass
class Room:
    host: object
    guest: object = None
    created: float = field(default_factory=time.monotonic)
    paired: bool = False


class Relay:
    def __init__(self, max_rooms=100, max_per_ip=8, waiting_seconds=300):
        self.rooms = {}
        self.connections = Counter()
        self.max_rooms = max_rooms
        self.max_per_ip = max_per_ip
        self.waiting_seconds = waiting_seconds

    async def send(self, socket, **message):
        await socket.send(json.dumps(message, separators=(',', ':')))

    async def reject(self, socket, message):
        await self.send(socket, relay='error', message=message)
        await socket.close(code=1008, reason=message[:100])

    async def handle(self, socket):
        ip = socket.remote_address[0] if socket.remote_address else 'unknown'
        if self.connections[ip] >= self.max_per_ip:
            await self.reject(socket, 'Too many connections. Try again later.')
            return
        self.connections[ip] += 1
        code = None
        room = None
        admitted = False
        host = False
        rate = deque()
        try:
            raw = await asyncio.wait_for(socket.recv(), 10)
            if not isinstance(raw, str):
                await self.reject(socket, 'Text packets required.')
                return
            request = json.loads(raw)
            if not isinstance(request, dict) or request.get('version') != PROTOCOL:
                await self.reject(socket, 'Both players must use the latest game version.')
                return
            if request.get('kind') == 'create':
                if len(self.rooms) >= self.max_rooms:
                    await self.reject(socket, 'Relay is full. Try again later.')
                    return
                code = ''.join(secrets.choice(ALPHABET) for _ in range(8))
                while code in self.rooms:
                    code = ''.join(secrets.choice(ALPHABET) for _ in range(8))
                room = Room(socket)
                self.rooms[code] = room
                host = admitted = True
                await self.send(socket, relay='created', code=code, version=PROTOCOL)
            elif request.get('kind') == 'join':
                code = request.get('code', '')
                if not isinstance(code, str) or len(code) != 8:
                    await self.reject(socket, 'Enter the eight-character room code.')
                    return
                room = self.rooms.get(code.upper())
                code = code.upper()
                if room is None or time.monotonic() - room.created > self.waiting_seconds:
                    await self.reject(socket, 'Room not found or expired. Ask the host for a new code.')
                    return
                if room.guest is not None or room.paired:
                    await self.reject(socket, 'This room already has two players.')
                    return
                room.guest = socket
                room.paired = admitted = True
                await self.send(room.host, relay='paired', code=code, version=PROTOCOL)
                await self.send(socket, relay='paired', code=code, version=PROTOCOL)
            else:
                await self.reject(socket, 'Create or join a room first.')
                return

            while True:
                timeout = None if room.paired else max(.01, self.waiting_seconds - (time.monotonic() - room.created))
                raw = await asyncio.wait_for(socket.recv(), timeout)
                if not isinstance(raw, str):
                    await self.reject(socket, 'Text packets required.')
                    return
                message = json.loads(raw)
                now = time.monotonic()
                while rate and now - rate[0] > 1:
                    rate.popleft()
                rate.append(now)
                if len(rate) > 120:
                    await self.reject(socket, 'Packet rate limit exceeded.')
                    return
                allowed = HOST_MESSAGES if host else GUEST_MESSAGES
                if not isinstance(message, dict) or message.get('version') != PROTOCOL or message.get('kind') not in allowed or 'relay' in message:
                    await self.reject(socket, 'Invalid game message for this player.')
                    return
                other = room.guest if host else room.host
                if other is not None:
                    await other.send(raw)
        except asyncio.TimeoutError:
            await self.reject(socket, 'Room or connection timed out. Create a new room.')
        except (ValueError, TypeError):
            await self.reject(socket, 'Invalid JSON message.')
        except ConnectionClosed:
            pass
        finally:
            self.connections[ip] -= 1
            if self.connections[ip] <= 0:
                del self.connections[ip]
            if admitted and room is not None:
                if self.rooms.get(code) is room:
                    self.rooms.pop(code, None)
                other = room.guest if host else room.host
                if other is not None:
                    try:
                        await self.send(other, relay='closed', message='The other player left the room.')
                        await other.close(code=1000, reason='Room closed')
                    except ConnectionClosed:
                        pass


def health(connection, request):
    if request.path == '/health':
        return Response(200, 'OK', Headers({'Content-Type': 'text/plain'}), b'Velocity relay ready\n')


def create_server(relay, host='127.0.0.1', port=0):
    return serve(relay.handle, host, port, max_size=4096, max_queue=16, write_limit=16384,
                 ping_interval=20, ping_timeout=20, close_timeout=3, process_request=health)


async def main():
    relay = Relay(max_rooms=int(os.getenv('MAX_ROOMS', '100')))
    port = int(os.getenv('PORT', '10000'))
    async with create_server(relay, '0.0.0.0', port):
        print(f'Velocity private-room relay listening on {port}', flush=True)
        await asyncio.Future()


if __name__ == '__main__':
    asyncio.run(main())
