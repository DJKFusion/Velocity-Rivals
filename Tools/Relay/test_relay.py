import asyncio
import json
import unittest
from websockets.asyncio.client import connect
from websockets.exceptions import ConnectionClosed
from server import Relay, create_server, PROTOCOL


class RelayTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.relay = Relay()
        self.server = await create_server(self.relay)
        self.uri = 'ws://127.0.0.1:' + str(self.server.sockets[0].getsockname()[1])
        self.clients = []

    async def asyncTearDown(self):
        for client in self.clients:
            await client.close()
        self.server.close()
        await self.server.wait_closed()

    async def client(self, kind, **values):
        client = await connect(self.uri)
        self.clients.append(client)
        await client.send(json.dumps(dict(kind=kind, version=PROTOCOL, **values)))
        return client, json.loads(await asyncio.wait_for(client.recv(), 2))

    async def pair(self):
        host, created = await self.client('create')
        guest, paired = await self.client('join', code=created['code'])
        self.assertEqual('paired', paired['relay'])
        self.assertEqual('paired', json.loads(await host.recv())['relay'])
        return host, guest, created['code']

    async def test_create_join_and_bidirectional_game_packets(self):
        host, guest, code = await self.pair()
        self.assertEqual(8, len(code))
        for sender, receiver, packet in [
            (guest, host, dict(kind='hello', guestCar=2)),
            (host, guest, dict(kind='start', mode=1, round=1)),
            (guest, host, dict(kind='loaded', round=1)),
            (host, guest, dict(kind='state', score0=0, score1=0)),
            (guest, host, dict(kind='input', throttle=1)),
        ]:
            packet['version'] = PROTOCOL
            await sender.send(json.dumps(packet))
            self.assertEqual(packet, json.loads(await asyncio.wait_for(receiver.recv(), 2)))

    async def test_unknown_room_is_rejected(self):
        _, message = await self.client('join', code='ABCDEFGH')
        self.assertEqual('error', message['relay'])
        self.assertEqual(0, len(self.relay.rooms))

    async def test_third_player_cannot_replace_guest(self):
        host, guest, code = await self.pair()
        _, message = await self.client('join', code=code)
        self.assertEqual('error', message['relay'])
        await guest.send(json.dumps(dict(kind='input', version=PROTOCOL, throttle=.5)))
        self.assertEqual(.5, json.loads(await host.recv())['throttle'])

    async def test_guest_cannot_send_authoritative_state(self):
        host, guest, _ = await self.pair()
        await guest.send(json.dumps(dict(kind='state', version=PROTOCOL, score1=999)))
        self.assertEqual('error', json.loads(await guest.recv())['relay'])
        self.assertEqual('closed', json.loads(await host.recv())['relay'])

    async def test_disconnect_closes_room_and_notifies_other_player(self):
        host, guest, code = await self.pair()
        await guest.close()
        self.assertEqual('closed', json.loads(await asyncio.wait_for(host.recv(), 2))['relay'])
        self.assertNotIn(code, self.relay.rooms)

    async def test_bad_protocol_is_rejected_before_room_creation(self):
        client = await connect(self.uri); self.clients.append(client)
        await client.send(json.dumps(dict(kind='create', version=1)))
        self.assertEqual('error', json.loads(await client.recv())['relay'])
        self.assertEqual(0, len(self.relay.rooms))

    async def test_oversized_message_disconnects(self):
        host, guest, _ = await self.pair()
        await guest.send('x' * 5000)
        with self.assertRaises(ConnectionClosed):
            await guest.recv()
        self.assertEqual('closed', json.loads(await asyncio.wait_for(host.recv(), 2))['relay'])

    async def test_room_capacity_limit(self):
        self.relay.max_rooms = 1
        _, first = await self.client('create')
        _, second = await self.client('create')
        self.assertEqual('created', first['relay'])
        self.assertEqual('error', second['relay'])


if __name__ == '__main__':
    unittest.main(verbosity=2)
