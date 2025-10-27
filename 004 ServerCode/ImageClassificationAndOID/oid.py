from flask import Flask, request, jsonify
import tensorflow as tf
from PIL import Image
import numpy as np
import io
import random

app = Flask(__name__)

# 모델 로드
kitchenModel = tf.keras.models.load_model('kitchen.keras',)
cleanModel = tf.keras.models.load_model('clean.keras',)
writeModel = tf.keras.models.load_model('WriteInstModel.keras',)

kitchen_names = ['coffeepot', 'container', 'cookpot', 'cup', 
    'fork', 'knife', 'ladle', 'mbowl', 'mcup', 'microwave', 
    'mspoon', 'pan', 'plate', 'ptowel', 'scale', 'spatula', 'spoon', 
    'strainer', 'toaster', 'tongs', 'whisk']

clean_names = ['airgun', 'broom', 'brush', 'bucket', 'dishcloth', 'duster', 'dustpan',
               'gloves', 'mop', 'mopsqueezer', 'sponge', 'sprayer', 'squrrgee', 
               'stickyroller', 'toiletbrush', 'vacuum']

write_names = ['ballpen', 'compass', 'crayon', 'eraser', 'fountainpen',
               'glue', 'highlighterpen', 'pencil', 'ruler', 'scissors', 'tape']

nextOID = random.randrange(1, 10000)

def preprocess_image(image):
    img = Image.open(io.BytesIO(image))
    img = img.resize((160, 160))
    img_array = tf.keras.preprocessing.image.img_to_array(img)
    img_array = tf.expand_dims(img_array, 0)
    return img_array

@app.route('/kitchen', methods=['POST']) # /predict에 post로 요청청
def kitchen():
    if 'image' not in request.files:
        return jsonify({'error': 'No image file provided'}), 400

    image_file = request.files['image'].read()
    processed_image = preprocess_image(image_file)

    predictions = kitchenModel.predict(processed_image)
    predicted_class_index = np.argmax(predictions[0])
    predicted_class = kitchen_names[predicted_class_index]
    confidence = float(predictions[0][predicted_class_index])

    return jsonify({
        'predicted_class': predicted_class,
        'confidence': confidence
    })

@app.route('/clean', methods=['POST']) # /predict에 post로 요청청
def clean():
    if 'image' not in request.files:
        return jsonify({'error': 'No image file provided'}), 400

    image_file = request.files['image'].read()
    processed_image = preprocess_image(image_file)

    predictions = cleanModel.predict(processed_image)
    predicted_class_index = np.argmax(predictions[0])
    predicted_class = clean_names[predicted_class_index]
    confidence = float(predictions[0][predicted_class_index])

    return jsonify({
        'predicted_class': predicted_class,
        'confidence': confidence
    })

@app.route('/write', methods=['POST']) # /predict에 post로 요청청
def write():
    if 'image' not in request.files:
        return jsonify({'error': 'No image file provided'}), 400

    image_file = request.files['image'].read()
    processed_image = preprocess_image(image_file)

    predictions = writeModel.predict(processed_image)
    predicted_class_index = np.argmax(predictions[0])
    predicted_class = write_names[predicted_class_index]
    confidence = float(predictions[0][predicted_class_index])

    return jsonify({
        'predicted_class': predicted_class,
        'confidence': confidence
    })

@app.route('/oid', methods=['GET'])
def oid():
    # DB에서 OID 얻어오기
    global nextOID
    nextOID += 1
    return jsonify({
        'oid' : nextOID
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=3333, debug=True)