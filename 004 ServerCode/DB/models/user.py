from extensions import db
from werkzeug.security import generate_password_hash, check_password_hash

class User(db.Model):
    __tablename__ = 'user'

    uid = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(20), nullable=False)
    email = db.Column(db.String(255), unique=True, nullable=False)
    password_hash = db.Column(db.String(255), nullable=False)
    level = db.Column(db.Integer, nullable=False)
    exp = db.Column(db.Float, nullable=False)
    money = db.Column(db.Integer, nullable=False)
    boxSize = db.Column(db.Integer, nullable=False)


    # 관계 설정
    # User와 Instance는 1:N 관계
    # cascade 사용을 위해선 DB엔진(InnoDB)를 사용해야함 
    instances = db.relationship('Instance', backref='user', cascade="all, delete", passive_deletes=True)

    @property
    def password(self):
        raise AttributeError('password is not a readable attribute')

    # @password.setter
    # def password(self, password):
    #     self.password_hash = generate_password_hash(password)

    def check_password(self, password):
        return check_password_hash(self.password_hash, password)