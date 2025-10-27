from flask import Flask, request, jsonify
import tensorflow as tf
from PIL import Image
import numpy as np
import io

app = Flask(__name__)

# 모델 로드
model = tf.keras.models.load_model('WriteInstModel.keras',)

# WriteInst
class_names = ['ballpen', 'compass', 'crayon', 'eraser', 'fountainpen',
               'glue', 'highlighterpen', 'pencil', 'ruler', 'scissors', 'tape']

#Kitchen
class_names1 = ['coffeepot', 'container', 'cookpot', 'cup', 
    'fork', 'knife', 'ladle', 'mbowl', 'mcup', 'microwave', 
    'mspoon', 'pan', 'plate', 'ptowel', 'scale', 'spatula', 'spoon', 
    'strainer', 'toaster', 'tongs', 'whisk']

#Clean
class_names2 = ['airgun', 'broom', 'brush', 'bucket', 'dishcloth', 'duster', 'dustpan',
               'gloves', 'mop', 'mopsqueezer', 'sponge', 'spray', 'squeezer', 
               'tapecleaner', 'toiletbrush', 'vacuum']

def preprocess_image(image):
    img = Image.open(io.BytesIO(image))
    img = img.resize((160, 160))
    img_array = tf.keras.preprocessing.image.img_to_array(img)
    img_array = tf.expand_dims(img_array, 0)
    return img_array

@app.route('/predict', methods=['POST']) # /predict에 post로 요청청
def predict():
    if 'image' not in request.files:
        return jsonify({'error': 'No image file provided'}), 400

    image_file = request.files['image'].read()
    processed_image = preprocess_image(image_file)

    predictions = model.predict(processed_image)
    predicted_class_index = np.argmax(predictions[0])
    predicted_class = class_names[predicted_class_index]
    confidence = float(predictions[0][predicted_class_index])

    return jsonify({
        'predicted_class': predicted_class,
        'confidence': confidence
    })

if __name__ == '__main__':
    print(tf.__version__)
    app.run(host='0.0.0.0', port=3335, debug=True) # localhost:3334 에 오픈
    # kitchen 3333, clean 3334, writeinst 3335
    # host=0.0.0.0 이 없어서 외부에서 접근이 안됬던거노..