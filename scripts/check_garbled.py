# -*- coding: utf-8 -*-
import re, sys

FILE_PATH = r'D:\Users\huang\文档\HBuilderProjects\通行证查询app\pages\card_share\card_share.vue'

with open(FILE_PATH, 'r', encoding='utf-8') as f:
    content = f.read()

# Find remaining garbled lines
lines = content.split('\n')
found = False
for i, line in enumerate(lines):
    # Common mojibake char ranges for UTF-8 Chinese interpreted as GBK->UTF8
    if re.search(r'[\u9c80-\u9fff]', line) or re.search(r'鐤|鐢|鐐|鍒|鍏|鍚|鍗|鍛|鍜|鍝|鍞|鍟|鍥|鍦|鎀|鎁|鎂|鎃|鎄|鎅|鎆|鎇|鎈|鎊|鎋|鎍|鎏|鎐|鎑|鎓|鎔|鎕|鎖|鎗|鎘|鎙|鎚|鎛|鎝|鎟|鎠|鎡|鎣|鎤|鎦|鎩|鎫|鎬|鎭|鎮|鎯|鎱|鎴|鎵|鎶|鎷|鎸|鎹|鎺|鎻|鎼|鎽|鎾|鎿|鏀|鏁|鏂|鏃|鏄|鏅|鏆|鏇|鏈|鏉|鏊|鏌|鏍|鏐|鏒|鏓|鏔|鏖|鏗|鏙|鏜|鏝|鏞|鏟|鏠|鏡|鏢|鏣|鏤|鏥|鏧|鏩|鏪|鏫|鏬|鏮|鏯|鏰|鏱|鏲|鏳|鏶|鏸|鏹|鏺|鏼|鏽|鏾|鏿|鐀|鐁|鐃|鐅|鐆|鐇|鐈|鐊|鐌|鐍|鐏|鐑|鐒|鐔|鐕|鐗|鐘|鐙|鐚|鐜|鐞|鐟|鐠|鐢|鐣|鐤|鐥|鐦|鐧|鐨|鐩|鐪|鐫|鐭|鐮|鐯|鐳|鐴|鐵|鐶|鐷|鐹|鐺|鐿|鑀|鑁|鑂|鑄|鑅|鑆|鑇|鑈|鑉|鑊|鑌|鑍|鑏|鑑|鑓|鑔|鑖|鑗|鑙|鑛|鑜|鑞|鑟|鑢|鑣|鑤|鑥|鑦|鑧|鑩|鑪|鑬|鑭|鑮|鑯|鑰|鑱|鑲|鑳|鑴|鑵|鑶|鑷|鑸|鑹|鑺|鑻|鑼|鑽|鑾|鑿', line):
        clean = line.strip()
        if len(clean) > 120:
            clean = clean[:120] + '...'
        sys.stdout.buffer.write(f'L{i+1}: {clean}\n'.encode('utf-8'))
        found = True

if not found:
    sys.stdout.buffer.write(b'No garbled text remaining!\n')
