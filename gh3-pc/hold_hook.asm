
pushfd
pushad
lea esi, [esp+36]
sub esp, 16
movdqu [esp], xmm0
mov ebx, 268439552
cmp dword ptr [ebx], 1
jne done
test ebp, ebp
jne done
cmp dword ptr [esi+0x40], 0
jne done
mov edi, dword ptr [0xa90cd4]
and edi, 0x11111
test edi, edi
je done
mov ecx, dword ptr [0xa90c80]
test ecx, ecx
js done
cmp ecx, dword ptr [0xa90c88]
jae done
mov eax, dword ptr [0xa9107c]
test eax, eax
je done
cmp ecx, dword ptr [eax+4]
jae done
cmp dword ptr [eax+4], 1
je single_chart
mov eax, dword ptr [eax+8]
mov eax, dword ptr [eax+ecx*4]
jmp note_array
single_chart:
mov eax, dword ptr [eax+8]
note_array:
test eax, eax
je done
cmp dword ptr [eax+4], 7
jb done
mov eax, dword ptr [eax+8]
test eax, eax
je done
cvtsi2ss xmm0, dword ptr [eax]
comiss xmm0, dword ptr [esi+0x24]
jp done
ja done
xor edx, edx
cmp dword ptr [eax+4], 0
jle red
or edx, 0x10000
red:
cmp dword ptr [eax+8], 0
jle yellow
or edx, 0x1000
yellow:
cmp dword ptr [eax+12], 0
jle blue
or edx, 0x100
blue:
cmp dword ptr [eax+16], 0
jle orange
or edx, 0x10
orange:
cmp dword ptr [eax+20], 0
jle compare
or edx, 1
compare:
cmp edx, edi
jne done
mov dword ptr [esi+0x28], 1
mov dword ptr [esi+0x80], edi
mov byte ptr [esi+0x17], 1
mov dword ptr [ebx+4], edi
movss dword ptr [ebx+8], xmm0
mov dword ptr [ebx+12], ecx
mov eax, dword ptr [0xa9107c]
mov dword ptr [ebx+16], eax
mov dword ptr [ebx+20], 1
done:
movdqu xmm0, [esp]
add esp, 16
popad
popfd
mov ecx, dword ptr [esp+0x28]
test ecx, ecx
jmp 4394871

; Miss callback helper at code offset 0x200

pushfd
pushad
lea esi, [esp+36]
sub esp, 16
movdqu [esp], xmm0
mov ebx, 268439552
cmp dword ptr [ebx], 1
jne normal
cmp dword ptr [ebx+20], 1
jne normal
cmp dword ptr [esi+4], 0
jne normal
mov eax, dword ptr [esi]
cmp eax, 0x431354
je caller_ok
cmp eax, 0x431378
jne normal
caller_ok:
mov eax, dword ptr [0xa90cd4]
and eax, 0x11111
cmp eax, dword ptr [ebx+4]
jne normal
mov eax, dword ptr [0xa9107c]
cmp eax, dword ptr [ebx+16]
jne normal
mov eax, dword ptr [ebx+12]
inc eax
cmp eax, dword ptr [0xa90c80]
jne normal
movss xmm0, dword ptr [ebx+8]
comiss xmm0, dword ptr [0xa90c60]
jp normal
jb normal
comiss xmm0, dword ptr [esi+8]
jp normal
ja normal
mov dword ptr [ebx+20], 0
movdqu xmm0, [esp]
add esp, 16
popad
popfd
ret
normal:
movdqu xmm0, [esp]
add esp, 16
popad
popfd
push ebp
mov ebp, 1
jmp 4392470
