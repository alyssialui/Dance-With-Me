   M S B A M L   `   `   `  ����   KinectCoordinateMapping%    KinectCoordinateMapping.Teacher   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$  Teacher�� � WindowStyle$ 
ToolWindow=� � Left$ 0��
 � Top$ 0��#��� ��#��� ��.���  � ColumnDefinitions �� $
��225* ��� $
��369* ��� $	��70* � � RowDefinitions �� $��4* ��� $��6* �.+��� -   $��camera��$
��Auto��$
��Auto��$��0��$��0��$��2��$��2���� -   $��canvas��$
��Auto��$
��Auto��$��0��$��0��$��2��$��2���� -   $��Toggle Skeleton��$
��Left=�$��20��$	��100��$��
16,310,0,0q�$	��Top=�$��1��$��2���� -    ��Name$ 
btn_Dance1��$��Dance 1��$
��Left=�$��20��$	��100��$��	16,12,0,0q�$	��Top=�$��2���� -   $ 
btn_Dance2��$��Dance 2��$
��Left=�$��20��$	��100��$��	16,37,0,0q�$	��Top=�$��2���� -   $ 
btn_Dance3��$��Dance 3��$
��Left=�$��20��$	��100��$��	16,62,0,0q�$	��Top=�$��2���� -   $ 
btn_Dance4��$��Dance 4��$
��Left=�$��20��$	��100��$��	16,87,0,0q�$	��Top=�$��2���� -	   $ btn_TeacherMode��$��Teacher Mode��$
��Left=�$��20��$	��100��$��
16,285,0,0q�$	��Top=�$��1��$��2���� -
   $ btn_StudentMode��$��Student Mode��$
��Left=�$��20��$	��100��$��
16,260,0,0q�$	��Top=�$��1��$��2���� -   $ btn_TeacherRecord��$��Record Limb Angles��$
��Left=�$��20��$	��120��$��5,37,0,0q�$	��Top=�$��1��$��2���� -    ��Name$ 
cbox_Audio��$��Audio��$��2��$��
5,230,5,22q��� -   $ 
cbox_Video��$��Video��$��2��$��	5,248,5,4q��� -    ��Name$ 	lbl_Score��$��Score: -��$��Right=�$��	0,10,40,0q�$	��Top=�$��36(�$
��Bold"�$��1���� -   	 ��Name$	 cmbox_dances��$��1��$��2��$
��Left=�$��	15,10,0,0q�$	��Top=�$	��100��$��20��
 �� SelectedIndex$
 0��.@��� $��Dance 1���� $��Dance 2���� $��Dance 3���� $��Dance 4��� -    �Name$ 	videoGrid��$��0��$��0��$
��Auto��$
��Auto��
������� �� $	��10* ��� $��1* �.+�r� -    r�Name$ video��$
��Auto��$��0��$��0��$
��Auto�� r� LoadedBehavior$ Manual=� J� RenderTransformOrigin$ 0.5,0.5&�z���  �� ScaleX$ -1Q��� -   $ 	btn_Start��$
��Play��$
��Left=�$��20��$��50��$��	299,2,0,0q�$	��Top=�$��1���� -   $ btn_Stop��$
��Stop��$
��Left=�$��20��$��50��$��	407,2,0,0q�$	��Top=�$��1���� -   $ 	btn_Pause��$��Pause��$
��Left=�$��20��$��50��$��	353,2,0,0q�$	��Top=�$��1���� -   $��sliProgress��$
��Left=�$��8,3,0,0q�$��1��$	��Top=�$	��229���� -   $ lbl_DanceName��$��-��$��Right=�$��0,0,35,0q�$	��Top=�
�������$��20(�$
��Bold"��� -   $ btn_Mute��$
��Mute��$
��Left=�$��20��$��50��$��	245,2,0,0q�$	��Top=�$��1��