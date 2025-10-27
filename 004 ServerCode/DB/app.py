from flask import Flask
from extensions import db
from config import DB_USERNAME, DB_PASSWORD, DB_HOST, DB_NAME, DB_PORT
from flask_migrate import Migrate
from routes import user_routes, session_routes, cache_routes, instance_routes, shop_routes
import models
import os

app = Flask(__name__)

# 업로드 디렉토리 경로 설정
app.config['UPLOAD_FOLDER'] = os.path.join(os.getcwd(), 'uploads')

# 디렉토리 경로가 없으면 생성
if not os.path.exists(app.config['UPLOAD_FOLDER']):
    os.makedirs(app.config['UPLOAD_FOLDER'])


# Flask-SQLAlchemy 설정
app.config['SQLALCHEMY_DATABASE_URI'] = f'mysql://{DB_USERNAME}:{DB_PASSWORD}@{DB_HOST}:{DB_PORT}/{DB_NAME}'
app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False  # 비효율적인 이벤트 추적 끄기

# SQLAlchemy 객체 생성
db.init_app(app)

# Flask-Migrate 객체 생성
migrate = Migrate(app, db)


@app.route('/')
def index():
    return '<h1>Flask 서버가 정상적으로 작동 중입니다.</h1>'

# 블루프린트 등록
app.register_blueprint(user_routes.user_bp)
app.register_blueprint(session_routes.session_bp)
app.register_blueprint(cache_routes.cache_bp)
app.register_blueprint(instance_routes.instance_bp)
app.register_blueprint(shop_routes.shop_bp)

if __name__ == '__main__':
    app.run(debug=True)
