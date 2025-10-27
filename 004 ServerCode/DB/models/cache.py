from extensions import db

class Cache(db.Model):
    __tablename__ = 'cache'
    
    id = db.Column(db.Integer, primary_key=True)
    uid = db.Column(db.Integer, nullable=False)
    oid = db.Column(db.Integer, nullable=False)