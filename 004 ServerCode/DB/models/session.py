from extensions import db

# sid 정의 시 필요하면 해쉬나 UUID 사용

class Session(db.Model):
    __tablename__ = 'session'

    sid = db.Column(db.String(255), primary_key=True)
    uid = db.Column(db.Integer, db.ForeignKey('user.uid'), nullable=False)
    expire = db.Column(db.DateTime, nullable=False)
    