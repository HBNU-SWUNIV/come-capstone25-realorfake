from flask import Flask, request, jsonify
from flask_mysqldb import MySQL
import datetime

app = Flask(__name__)
# DB 정보 넣기
app.config['MYSQL_HOST'] = 'localhost'
app.config['MYSDQL_USER'] = 'root'
app.config['MYSQL_PASSWORD'] = 'password'
app.config['MYSQL_DB'] = 'dbname'

mysql = MySQL(app)

# 테이블 이름 넣기
userDbTableName = ''
sessionDbTableName = ''

# 요청마다 SessionDB의 sid 값 비교 필요. 
# sid 값이 존재하지 않으면 유효하지 않은 요청 -> success 필드 false 반환 -> 클라이언트는 자동 로그아웃

# 요청마다 SessionDB의 expire 값 비교 필요
# Session DB의 expire 값을 현재 시간과 비교 후 시간 초과 시 success 필드 false 반환 후 정보 삭제
# -> 로그아웃
# Session DB의 expire 값을 현재 시간과 비교 후 시간 안지났으면  success 필드 true 반환 후 expire 값 현재 시간 대비 30분 뒤로 갱신
# -> expire 값 갱신

'''
/register

INPUT : name, email, pasword_hash(클라이언트에서 해시 해서 보냄)
OUTPUT : success(성공 여부), fail(실패 원인)

'''
@app.route('/register', method=['POST'])
def register():
    conn = mysql.connection.cursor()
    name = request['name']
    email = request['email']
    password_hash = request['password_hash']
    conn.execute("SELECT 1 FROM %s WHERE name=%s OR email=%s LIMIT 1", (userDbTableName, name, email))
    result = conn.fetchone()
    if (result):
        # 이미 존재하는 이름, 이메일
        return jsonify({
            'success' : 'false',
            'fail' : 'Already Exist Name or Email'
        })
    
    conn.execute("INSERT INTO %s (name, email, password_hash, level, exp, money, box_size) VALUES (%s, %s, %s, %s, %s, %s, %s)", 
                 (userDbTableName, name, email, password_hash, 1, 0, 100, 100))
    conn.commit()
    return jsonify({
        'success' : 'true'
    })


'''
/login

INPUT : name, password_hash
OUTPUT : success(성공 여부), sid(세션 ID), expire(세션 만료 시간), uid(유저 ID), level(유저 레벨), exp(유저 경험치), money(유저 재화), box_size(아이템 칸)

'''
@app.route('/login', method=['POST'])
def login():
    conn = mysql.connection.cursor()
    name = request['name']
    password_hash = request['password_hash']
    conn.execute("SELECT * FROM %s WHERE name=%s AND password_hash=%s", (userDbTableName, name, password_hash))
    result = conn.fetchone()
    if (result):
        # SessionDB에 유저 정보 추가
        # expire 은 현재 시간 대비 30분 이후로 설정
        uid = result[0]
        # sid는 해시값 이용 ex) 현재시간+uid 등..
        # uid는 위에 execute에서 SELECT로 가져와서 넣으면 베스트
        # expire는 현재 시간 + 30분

        sid = hash(uid + datetime.datetime.now())

        conn.execute("INSERT INTO %s (sid, uid, expire) VALUES (%s, %s, %s)", (sessionDbTableName, sid, uid, datetime.datetime.now() + 1800))

        conn.commit()
        # 추가한 정보 기반 반환
        return jsonify({
            'success' : 'true',
            'sid' : 'sessionid',
            'expire' : 'expireTime',
            'uid' : 'userId',
            'level' : 'userLevel',
            'exp' : 'userExp',
            'money' : 'userMoney',
            'box_size' : 'userBoxSize'
        })
    
    return jsonify({
        'success' : 'false'
    })


'''
/logout

INPUT : sid
OUTPUT : success(성공 여부)

- sid만 받으면 보안에 문제가 될 수 있을까..?

'''
@app.route('/logout', method=['POST'])
def logout():
    conn = mysql.connection.cursor()
    sid = request['sid']
    # Session DB에서 로그인 정보 삭제
    conn.execute("DELETE FROM %s WHERE sid=%s", (sessionDbTableName, sid))
    conn.commit()

    return jsonify({
        'success' : 'true'
    })

# 기타 추가 로직
'''
/increaseexp

INPUT : sid, uid, exp
OUTPUT : success(성공 여부), sid(세션 ID), expire(세션 만료 시간), uid(유저 ID), level(유저 레벨), exp(유저 경험치), money(유저 재화), box_size(아이템 칸)

exp 값 만큼 유저 exp를 더한다.
만약 exp의 값이 100을 넘어간다면 level을 1 올려준다
'''
@app.route('/increaseexp', method=['POST'])
def increaseexp():
    conn = mysql.connection.cursor()
    sid = request['sid']
    uid = request['uid']
    rexp = request['exp']

    # Session 관련 확인
    conn.execute("SELECT * FROM %s WHERE sid=%s AND uid=%s", (sessionDbTableName, sid, uid))
    result = conn.fetchone()

    if result:
        sid = result[0]
        uid = result[1]
        expire = result[2]

        dt_obj = datetime.datetime.combine(expire, datetime.time.min)
        now = datetime.datetime.now()

        if dt_obj - now <= 0:
            # 시간 만료
            return jsonify({
                'success' : 'false'
            })
        else:
            conn.execute("UPDATE %s SET expire=%s WHERE sid=%s", (sessionDbTableName, now + 1800, sid))
            conn.commit()
            conn.execute("SELECT * FROM %s WHERE uid=%s", (userDbTableName, uid))
            result = conn.fetchone()
            level = result[4]
            exp = result[5]
            money = result[6]
            box_size = result[7]
            exp += rexp
            if (exp >= 100):
                level += 1
                exp -= 100
            
            conn.execute("UPDATE %s SET level=%s, exp=%s WHERE uid=%s", (userDbTableName, level, exp, uid))
            conn.commit()

            return jsonify({
                'success' : 'true',
                'sid' : sid,
                'expire' : now + 1800,
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
@app.route('/money', method=['POST'])
def money():
    conn = mysql.connection.cursor()
    sid = request['sid']
    uid = request['uid']
    rmoney = request['money']

    # Session 관련 확인
    conn.execute("SELECT * FROM %s WHERE sid=%s AND uid=%s", (sessionDbTableName, sid, uid))
    result = conn.fetchone()

    # Session이 존재할 경우
    if result:
        sid = result[0]
        uid = result[1]
        expire = result[2]

        dt_obj = datetime.datetime.combine(expire, datetime.time.min)
        now = datetime.datetime.now()

        # Expire 지났는지 확인
        if dt_obj - now <= 0:
            # 시간 만료
            return jsonify({
                'success' : 'false',
                'fail' : 'Expired Session'
            })
        else:
            # Expire 안지남, Expire 값 업데이트
            conn.execute("UPDATE %s SET expire=%s WHERE sid=%s", (sessionDbTableName, now + 1800, sid))
            conn.commit()

            # UserDB에서 유저 정보 가져오기
            conn.execute("SELECT * FROM %s WHERE uid=%s", (userDbTableName, uid))
            result = conn.fetchone()
            level = result[4]
            exp = result[5]
            money = result[6]
            box_size = result[7]
            money += rmoney
            # rmoney가 음수라서 돈이 모자라게 되면 false 반환
            if (money <= 0):
                return jsonify({
                    'success' : 'false',
                    'fail' : 'not enough money'
                })
            
            conn.execute("UPDATE %s SET moeny=%s WHERE uid=%s", (userDbTableName, money, uid))
            conn.commit()

            return jsonify({
                'success' : 'true',
                'sid' : sid,
                'expire' : now + 1800,
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
            'fail' : 'Invalid Session'
        })