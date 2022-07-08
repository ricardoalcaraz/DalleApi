"""
It sends a python dict (producer, some_id, count)
to REDIS STREAM (using the xadd method)

Usage:
  PRODUCER=Roger MESSAGES=10 python producer.py
"""
from os import environ
from redis import Redis
from uuid import uuid4
from time import sleep
from min_dalle import MinDalle
import argparse
import logging
import image_rpc_pb2_grpc
import image_rpc_pb2


parser = argparse.ArgumentParser()
parser.set_defaults(mega=False)
parser.add_argument('--seed', type=int, default=-1)
parser.add_argument('--grid-size', type=int, default=1)
parser.add_argument('--image-path', type=str, default='generated')
parser.add_argument('--models-root', type=str, default='pretrained')

stream_name = environ.get("STREAM", "jarless-1")
producer = environ.get("PRODUCER", "user-1")
sleep_ms = 5000

model = MinDalle(is_mega=True, models_root='./pretrained')

def connect_to_redis():
    hostname = 'localhost'
    port = 6379

    redis = Redis(hostname, port, retry_on_timeout=True)
    message = redis.xread({stream_name: '$q'}, count=1, block=sleep_ms)
    
    return redis


def get_data(redis: Redis[bytes]):
    while True:
        try:
            message = redis.xread({stream_name: 0}, None, block=sleep_ms)

            if message:
                print(message)
                data = image_rpc_pb2.TextRequest.ParseFromString(message)
                print(data)
                image = model.generate_image(data, 0, 1, is_verbose=True)
                image.save('./test.jpg')
            
            sleep(0.001)
        except ConnectionError as e:
            print("ERROR REDIS CONNECTION: {}".format(e))

logging.basicConfig(level=logging.INFO)

if __name__ == "__main__":
    connection = connect_to_redis()
    get_data(connection)