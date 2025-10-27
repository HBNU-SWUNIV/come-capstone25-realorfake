from flask import Blueprint, request, jsonify
from models import User, Session, Instance
from extensions import db
from datetime import datetime, timedelta, time

user_bp = Blueprint("user", __name__, url_prefix="/user")

# 사용자 생성
# @user_bp.route("/create", methods=["POST"])
# def create_user():
#     data = request.get_json()
#     print(data)
#     if not data:
#         return jsonify({"error": "Invalid input"}), 400
    
#     new_user = User(
#         name=data["_name"], 
#         password=data["_password"], 
#         email=data["_email"], 
#         level=1, 
#         exp=0.0, 
#         money=0, 
#         boxSize=10
#     )
#     # level, exp, money, boxSize는 기본값으로 설정
#     # uid는 자동 증가로 설정되어 있으므로 명시적으로 지정할 필요 없음

#     # 이메일 중복 체크
#     existing_user = User.query.filter_by(email=data["_email"]).first()
#     if existing_user:
#         return jsonify({"error": "Email already exists!"}), 400  # 이메일 중복
#     else:
#         # 데이터베이스에 사용자 추가
#         db.session.add(new_user)
#         db.session.commit()
#         return jsonify({"message": "user created"}), 201

'''
/register

INPUT : name, email, pasword_hash(클라이언트에서 해시 해서 보냄)
OUTPUT : success(성공 여부), fail(실패 원인)

'''
@user_bp.route('/register', methods=['POST'])
def register():
    # conn = db.connection.cursor()
    data = request.get_json()
    new_user = User(
        name=data["id"], 
        password_hash=data["password_hash"], 
        email=data["email"], 
        level=1, 
        exp=0.0, 
        money=0, 
        boxSize=10
    )

    # level, exp, money, boxSize는 기본값으로 설정
    # uid는 자동 증가로 설정되어 있으므로 명시적으로 지정할 필요 없음

    # 이메일 중복 체크
    existing_user = User.query.filter_by(email=data["email"]).first()
    if existing_user:
        return jsonify({'success' : 'false',
                        'fail' : 'Already Exist Email'}), 400  # 이메일 중복
    else:
        # 데이터베이스에 사용자 추가
        db.session.add(new_user)
        db.session.commit()
        return jsonify({'success' : 'true'}), 201

    # name = request['name']
    # email = request['email']
    # password_hash = request['password_hash']
    # conn.execute("SELECT 1 FROM %s WHERE name=%s OR email=%s LIMIT 1", (userDbTableName, name, email))
    # result = conn.fetchone()
    # if (result):
    #     # 이미 존재하는 이름, 이메일
    #     return jsonify({
    #         'success' : 'false',
    #         'fail' : 'Already Exist Name or Email'
    #     }), 400
    
    # conn.execute("INSERT INTO %s (name, email, password_hash, level, exp, money, box_size) VALUES (%s, %s, %s, %s, %s, %s, %s)", 
    #              (userDbTableName, name, email, password_hash, 1, 0, 100, 100))
    # conn.commit()
    # return jsonify({
    #     'success' : 'true'
    # })


'''
/login

INPUT : name, password_hash
OUTPUT : success(성공 여부), sid(세션 ID), expire(세션 만료 시간), uid(유저 ID), level(유저 레벨), exp(유저 경험치), money(유저 재화), box_size(아이템 칸)

'''
@user_bp.route('/login', methods=['POST'])
def login():
    data = request.get_json()
    name = data['id']
    password_hash = data['password_hash']

    result = User.query.filter_by(name=name, password_hash=password_hash).first()
    if (result):
        uid = result.uid

        # 세션에 이미 존재하면 기존정보 삭제
        Session.query.filter_by(uid=uid).delete()
        db.session.commit()

        # sid는 해시값 이용 ex) 현재시간+uid 등..
        # uid는 위에 execute에서 SELECT로 가져와서 넣으면 베스트
        # expire는 현재 시간 + 30분

        now = datetime.now()
        sid = hash(str(uid) + str(now))

        # SessionDB에 유저 정보 추가
        # expire 은 현재 시간 대비 30분 이후로 설정
        # conn.execute("INSERT INTO %s (sid, uid, expire) VALUES (%s, %s, %s)", (sessionDbTableName, sid, uid, datetime.datetime.now() + 1800))
        # conn.commit()
        new_session = Session(
            sid=sid,
            uid=uid,
            expire=now + timedelta(seconds=1800) 
        )

        db.session.add(new_session)
        db.session.commit()

        instances = Instance.query.filter_by(uid=uid).all()
        oid_list = []
        for instance in instances:
            oid_list.append(instance.oid)

        # 추가한 정보 기반 반환
        return jsonify({
            'success' : 'true',
            'sid' : new_session.sid,
            'expire' : new_session.expire,
            'uid' : result.uid,
            'level' : result.uid,
            'exp' : result.exp,
            'money' : result.money,
            'box_size' : result.boxSize,
            'oid' : oid_list
        })
    
    return jsonify({
        'success' : 'false',
        'message': 'user does not exist'
    }), 400


'''
/logout

INPUT : sid
OUTPUT : success(성공 여부)

- sid만 받으면 보안에 문제가 될 수 있을까..?

'''
@user_bp.route('/logout', methods=['POST'])
def logout():
    # conn = db.connection.cursor()
    data = request.get_json()
    uid = data['uid']
    sid = data['sid']
    # sid = data['sid']
    # Session DB에서 로그인 정보 삭제
    # conn.execute("DELETE FROM %s WHERE sid=%s", (sessionDbTableName, sid))
    # conn.commit()

    session = Session.query.filter_by(uid=uid, sid=sid).first()
    if not session:
        return jsonify({"message": "Invalid access"}), 403

    db.session.delete(session)
    db.session.commit()
    return jsonify({'success' : 'true'}), 200
    

# 기타 추가 로직
'''
/increaseexp

INPUT : sid, uid, exp
OUTPUT : success(성공 여부), sid(세션 ID), expire(세션 만료 시간), uid(유저 ID), level(유저 레벨), exp(유저 경험치), money(유저 재화), box_size(아이템 칸)

exp 값 만큼 유저 exp를 더한다.
만약 exp의 값이 100을 넘어간다면 level을 1 올려준다
'''
@user_bp.route('/increaseexp', methods=['POST'])
def increase_exp():
    # conn = db.connection.cursor()
    data = request.get_json()
    sid = data['sid']
    uid = data['uid']
    rexp = float(data['exp'])

    # Session 관련 확인
    # conn.execute("SELECT * FROM %s WHERE sid=%s AND uid=%s", (sessionDbTableName, sid, uid))
    # result = conn.fetchone()

    session = Session.query.filter_by(sid=data['sid'], uid=data['uid']).first()

    if session:
        sid = session.sid
        uid = session.uid
        expire = session.expire

        now = datetime.now()

        if expire <= now:
            # 시간 만료
            return jsonify({
                'success' : 'false',
                'fail' : 'Expired Session'
            })
        else:
            # conn.execute("UPDATE %s SET expire=%s WHERE sid=%s", (sessionDbTableName, now + 1800, sid))
            # conn.commit()
            Session.query.filter_by(sid=data['sid']).update({
                'expire': now + timedelta(seconds=1800)
            })
            db.session.commit()

            # conn.execute("SELECT * FROM %s WHERE uid=%s", (userDbTableName, uid))
            # result = conn.fetchone()

            user = User.query.filter_by(uid=data['uid']).first()
            level = int(user.level)
            exp = float(user.exp)
            money = int(user.money)
            box_size = int(user.boxSize)
            exp += rexp
            if (exp >= 100): # 경험치 잔여량 계산로직 수정 필요
                level += 1
                exp -= 100
            
            User.query.filter_by(uid=data['uid']).update({
                'level': level,
                'exp': exp
            })
            db.session.commit()
            # conn.execute("UPDATE %s SET level=%s, exp=%s WHERE uid=%s", (userDbTableName, level, exp, uid))
            # conn.commit()

            return jsonify({
                'success' : 'true',
                'sid' : sid,
                'expire' : now + timedelta(seconds=1800),
                'uid' : uid,
                'level' : level,
                'exp' : exp,
                'money' : money,
                'box_size' : box_size
            })

    else:
        # UID에 존재하는 SID 존재 X, 잘못된 요청
        return jsonify({
            'success' : 'false'
        })

'''
/money

INPUT : sid, uid, money
OUTPUT : success(성공 여부), sid(세션 ID), expire(세션 만료 시간), uid(유저 ID), level(유저 레벨), exp(유저 경험치), money(유저 재화), box_size(아이템 칸)

money 값 만큼 유저 money를 더한다. (음수가 들어올 수도 잇음)
돈이 부족하다면 success : false 반환
'''
@user_bp.route('/money', methods=['POST'])
def add_money():
    # conn = db.connection.cursor()
    data = request.get_json()
    sid = data['sid']
    uid = data['uid']
    rmoney = int(data['money'])

    # Session 관련 확인
    session = Session.query.filter_by(sid=data['sid'], uid=data['uid']).first()
    # conn.execute("SELECT * FROM %s WHERE sid=%s AND uid=%s", (sessionDbTableName, sid, uid))
    # result = conn.fetchone()

    # Session이 존재할 경우
    if session:
        sid = session.sid
        uid = session.uid
        expire = session.expire

        now = datetime.now()

        # Expire 지났는지 확인
        if expire <= now:
            # 시간 만료
            return jsonify({
                'success' : 'false',
                'fail' : 'expired session'
            })
        else:
            # Expire 값 업데이트
            Session.query.filter_by(sid=data['sid']).update({
                'expire': now + timedelta(seconds=1800)
            })
            db.session.commit()
            # conn.execute("UPDATE %s SET expire=%s WHERE sid=%s", (sessionDbTableName, now + 1800, sid))
            # conn.commit()

            # UserDB에서 유저 정보 가져오기
            user = User.query.filter_by(uid=data['uid']).first()
            level = int(user.level)
            exp = float(user.exp)
            money = int(user.money)
            box_size = int(user.boxSize)
    
            money += rmoney
            # rmoney가 음수라서 돈이 모자라게 되면 false 반환
            if (money <= 0):
                return jsonify({
                    'success' : 'false',
                    'fail' : 'not enough money'
                })
            

            User.query.filter_by(uid=data['uid']).update({
                'money': money,
            })
            db.session.commit()
            # conn.execute("UPDATE %s SET moeny=%s WHERE uid=%s", (userDbTableName, money, uid))
            # conn.commit()

            return jsonify({
                'success' : 'true',
                'sid' : sid,
                'expire' : now + timedelta(seconds=1800),
                'uid' : uid,
                'level' : level,
                'exp' : exp,
                'money' : money,
                'box_size' : box_size
            })

    else:
        # UID에 존재하는 SID 존재 X, 잘못된 요청
        return jsonify({
            'success' : 'false',
            'fail' : 'invalid session'
        })





# uid로 사용자 조회
@user_bp.route("/<int:uid>", methods=["GET"])
def get_user(uid):
    user = User.query.get(uid)
    if not user:
        return jsonify({"message": "user not found"}), 404

    user_data = {
        "uid": user.uid,
        "name": user.name,
        "email": user.email,
        "level": user.level,
        "exp": user.exp,
        "money": user.money,
        "boxSize": user.boxSize
    }
    return jsonify(user_data), 200



# 사용자 정보 수정
@user_bp.route("/<int:uid>", methods=["PUT"])
def update_user(uid):
    data = request.get_json()
    user = User.query.get(uid)
    if not user:
        return jsonify({"message": "user not found"}), 404

    # 수정할 필드만 업데이트
    if "_name" in data:
        user.name = data["_name"]
    if "_password" in data:
        user.password = data["_password"]
    if "_email" in data:
        print(user.email, data["_email"])
        # 이메일 유효성 체크
        if data["_email"] == user.email:
            return jsonify({"warning": "Email not changed!"}), 400 # 기존 이메일

        existing_user = User.query.filter_by(email=data["_email"]).first()
        if existing_user and existing_user.uid != uid:
            return jsonify({"error": "Email already exists!"}), 400  # 이메일 중복
        
        user.email = data["_email"]
    if "_level" in data:
        user.level = data["_level"]  
    if "_exp" in data:
        user.exp = data["_exp"]
    if "_money" in data:
        user.money = data["_money"]
    if "_boxSize" in data:
        user.boxSize = data["_boxSize"]
        

    db.session.commit()
    return jsonify({"message": "user updated"}), 200

# 사용자 삭제
@user_bp.route("/<int:uid>", methods=["DELETE"])
def delete_user(uid):
    user = User.query.get(uid)
    if not user:
        return jsonify({"message": "user not found"}), 404

    db.session.delete(user)
    db.session.commit()
    return jsonify({"message": "user deleted"}), 200

