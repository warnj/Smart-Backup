   M S B A M L   `   `   `  ����  Smart_Backup    Smart_Backup.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$  Smart Backup��5   	   $	��700��6   $	��700��6+    � 
ResizeMode$ 	CanResize=�67    � Icon$ Save.ico��6N    � WindowStartupLocation$ CenterScreen=�6^   .��5      � ���	#FF2943B6 � RowDefinitions 5   
   �� 5      $	��200 �6   �� 5      $��90 �6   �� 5	      .+�5   
   � $��0,3,0,0q�6    � ColumnDefinitions 5      �� 5      $	��200 �6#   �� 5       5      �� 5      �� 5      �� 5      �� 5      .+�5      �� -   5         ��Name$ Source��5      $��0��6%   $��Source Folder To Clone��62   $��15,8,8,8q�6S   6e   �� 5      -   5        $ Destination��5      $��1��6*   $��Backup Destination Folder��67   $��15,8,8,8q�6[   6m   �� 5      -   5        $ Begin��5      $��2��6$   $��Begin Update��61   $��15,8,8,8q�6H   6Z   �� 5      -   5        $ Cancel��5      $��3��6%   $��Cancel��62   $��15,8,8,8q�6C   6U   �� 5      -   5         ��Name$ Backup_Source��5      $��1��6+   $��Empty��6;   $��10q�6K   ���	#FFBBE5FD$��Center=�6n   �� 5      -   5        $ Backup_Dest��5      $��1��6)   $��1��69   $��Empty��6F   $��10q�6V   ���	#FFBBE5FD$��Center=�6y   �� 5       -   5         ��Name$ Remove_Old_Destination��5       $��1��67   $��2��6G   	 d� 	IsChecked$
	 TrueY�6T   $=��7Delete Files in Backup Destination not in Backup Source��6e   $��10q�6�   ���	#FFDCF062$��Center=�6�   �� 5!      -	   5        $ Shutdown��5!      $&�� Shutdown when backup is complete��6)   ���	#FFDCF062$��10q�6k   $��1��6w   $��3��6�   $��Center=�6�   5%      �� 5)   
   $��1��6   .+�5*      �� $��Status:��6   $
��Left=�6&   $��33��6A   $	��Top=�6M   $��71��6e   
�������$
��Bold"�6�   $��18.667(�6�   
 �� Left$
 21��6�   
 �� Top$ 8��6�   �� 5+      -
   5        $ 	PanelText��5+      $&�� Select source and backup folders��6'   $
��Left=�6R   $��41��6m   $	��Top=�6y   $	��404��6�   ���	#FFF2F78E$��17.333(�6�   $
 97��6�   $ 9��6�   �� 5-      -   5        $ Update_Diff��5-      $ ��Calculate File Differences��6*   $
��Left=�6O   $��30��6j   $	��Top=�6v   $	��160��6�   $	
 294��6�   $ 54��6�    �� 	IsDefault . 6�   �� 5/      -   5         ��Name$ VolumeLabel��5/      $
��Left=�6-   $��18��6H   $
��Wrap=�6T   $��Files not Backed Up:��6h   $	��Top=�6�   $	��177��6�   ���	#FFDCF062$
 24��6�   $ 51��6�   �� 50      -   5        $ 
Diff_Files��50      $	��N/A��6(   $
��Left=�66   $��23��6Q   $	��Top=�6]   $��95��6u   ���	#FFDCF062$	
 217��6�   $ 46��6�   �� 52      -   5        $ Label��52      $
��Left=�6'   $��23��6B   $
��Wrap=�6N   $)��#Files in Destination not in Source:��6b   $	��Top=�6�   $	��214��6�   ���	#FFDCF062$
 24��6�   $ 69��6�   �� 53      -   5        $ Diff_Local_Files��53      $	��N/A��6.   $
��Left=�6<   $��28��6W   $	��Top=�6c   $��62��6{   ���	#FFDCF062$	
 217��6�   $ 64��6�   55      �� 57   
   -   5         ��Name$ 
infoTxtBox��57      $��2��6&    �� TextWrapping$ NoWrap=�63   $�� ��6I   $��10q�6Q   $ �� HorizontalScrollBarVisibility$
 Auto=�6]   " �� VerticalScrollBarVisibility$
 Auto=�6�    �� 
IsReadOnly . 
������� �� 	UndoLimit$ 0��6�   ��. 6�   5:      5;      